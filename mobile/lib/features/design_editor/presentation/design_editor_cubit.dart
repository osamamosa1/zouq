import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:image_picker/image_picker.dart';
import 'package:zouq/features/catalog/data/catalog_dtos.dart';
import 'package:zouq/features/catalog/data/catalog_repository.dart';
import 'package:zouq/features/design_editor/data/design_dtos.dart';
import 'package:zouq/features/design_editor/data/design_repository.dart';
import 'package:zouq/features/design_editor/domain/editor_element.dart';
import 'package:zouq/features/design_editor/domain/production_method.dart';
import 'package:zouq/features/orders/data/order_dtos.dart';
import 'package:zouq/features/orders/data/orders_repository.dart';

part 'design_editor_state.dart';

class DesignEditorCubit extends Cubit<DesignEditorState> {
  DesignEditorCubit(this._catalog, this._designs, this._orders) : super(const DesignEditorInitial());

  final CatalogRepository _catalog;
  final DesignRepository _designs;
  final OrdersRepository _orders;
  final ImagePicker _picker = ImagePicker();

  Future<void> load(String productId, {String? draftDesignId}) async {
    emit(const DesignEditorLoading());
    try {
      final config = await _catalog.getProductConfig(productId);
      DesignDto? draft;
      if (draftDesignId != null) {
        draft = await _designs.getDesign(draftDesignId);
      }

      final printingId = draft?.printingOptionId ??
          (config.printingOptions.isNotEmpty ? config.printingOptions.first.id : null);
      PrintingOptionDto? printing;
      if (printingId != null) {
        for (final p in config.printingOptions) {
          if (p.id == printingId) {
            printing = p;
            break;
          }
        }
      }
      final surfaces = _surfacesForPrinting(config, printing);
      final activeSurface = surfaces.isNotEmpty
          ? surfaces.first
          : (config.surfaces.isNotEmpty ? config.surfaces.first : null);

      final elements = <EditorElement>[];
      if (draft != null) {
        for (final e in draft.elements) {
          elements.add(
            EditorElement(
              id: 'el_${elements.length}_${DateTime.now().microsecondsSinceEpoch}',
              surfaceCode: e.surfaceCode,
              designAssetId: e.designAssetId,
              userUploadId: e.userUploadId,
              productionMethod: e.productionMethod.toLowerCase().contains('embroider')
                  ? ProductionMethod.embroidery
                  : ProductionMethod.printing,
              normX: e.normX,
              normY: e.normY,
              normWidth: e.normWidth,
              normHeight: e.normHeight,
              rotationDegrees: e.rotationDegrees,
              scale: e.scale,
              zIndex: e.zIndex,
              label: e.userUploadId != null ? 'Upload' : 'Design',
            ),
          );
        }
      }

      emit(
        DesignEditorReady(
          config: config,
          selectedFabricId: draft?.fabricId ??
              (config.fabrics.isNotEmpty ? config.fabrics.first.id : null),
          selectedCutId: draft?.cutStyleId ??
              (config.cuts.isNotEmpty ? config.cuts.first.id : null),
          selectedSizeId: draft?.productSizeId ??
              (config.sizes.isNotEmpty ? config.sizes.first.id : null),
          selectedPrintingId: printingId,
          activeSurfaceCode: activeSurface?.code,
          elements: elements,
          selectedElementId: elements.isNotEmpty ? elements.first.id : null,
          savedDesignId: draft?.id,
        ),
      );
    } catch (e) {
      emit(DesignEditorError(e.toString()));
    }
  }

  List<SurfaceDto> _surfacesForPrinting(ProductConfigDto config, PrintingOptionDto? printing) {
    if (printing == null) return config.surfaces;
    final codes = printing.includedSurfaceCodes.toSet();
    return config.surfaces.where((s) => codes.contains(s.code)).toList();
  }

  List<SurfaceDto> availableSurfaces(DesignEditorReady state) {
    PrintingOptionDto? printing;
    final pid = state.selectedPrintingId;
    if (pid != null) {
      for (final p in state.config.printingOptions) {
        if (p.id == pid) {
          printing = p;
          break;
        }
      }
    }
    return _surfacesForPrinting(state.config, printing);
  }

  void selectPrinting(String? printingId) {
    final current = state;
    if (current is! DesignEditorReady) return;
    final printing = current.config.printingOptions.where((p) => p.id == printingId).firstOrNull;
    final surfaces = _surfacesForPrinting(current.config, printing);
    final allowed = surfaces.map((s) => s.code).toSet();
    final kept = current.elements.where((e) => allowed.contains(e.surfaceCode)).toList();
    final active = surfaces.any((s) => s.code == current.activeSurfaceCode)
        ? current.activeSurfaceCode
        : (surfaces.isNotEmpty ? surfaces.first.code : null);
    final selectedStillValid =
        kept.any((e) => e.id == current.selectedElementId) ? current.selectedElementId : null;
    emit(
      current.copyWith(
        selectedPrintingId: printingId,
        activeSurfaceCode: active,
        elements: kept,
        selectedElementId: selectedStillValid,
        clearSelectedElement: selectedStillValid == null,
        clearQuote: true,
      ),
    );
  }

  void selectFabric(String? id) => _patchReady((s) => s.copyWith(selectedFabricId: id, clearQuote: true));
  void selectCut(String? id) => _patchReady((s) => s.copyWith(selectedCutId: id, clearQuote: true));
  void selectSize(String? id) => _patchReady((s) => s.copyWith(selectedSizeId: id, clearQuote: true));

  void setActiveSurface(String code) {
    _patchReady((s) => s.copyWith(activeSurfaceCode: code, clearQuote: true));
  }

  void addElement() {
    _patchReady((s) {
      final code = s.activeSurfaceCode;
      if (code == null) return s;
      final element = EditorElement.placeholder(surfaceCode: code, zIndex: s.elements.length);
      return s.copyWith(
        elements: [...s.elements, element],
        selectedElementId: element.id,
        clearQuote: true,
      );
    });
  }

  void addAssetElement({
    required String assetId,
    required String label,
  }) {
    _patchReady((s) {
      final code = s.activeSurfaceCode;
      if (code == null) return s;
      final element = EditorElement(
        id: 'asset_${DateTime.now().microsecondsSinceEpoch}',
        surfaceCode: code,
        designAssetId: assetId,
        productionMethod: ProductionMethod.printing,
        normX: 0.2,
        normY: 0.2,
        normWidth: 0.5,
        normHeight: 0.5,
        zIndex: s.elements.length,
        label: label,
      );
      return s.copyWith(
        elements: [...s.elements, element],
        selectedElementId: element.id,
        clearQuote: true,
      );
    });
  }

  void removeSelectedElement() {
    _patchReady((s) {
      final id = s.selectedElementId;
      if (id == null) return s;
      final next = s.elements.where((e) => e.id != id).toList();
      return s.copyWith(
        elements: next,
        clearSelectedElement: true,
        clearQuote: true,
      );
    });
  }

  void selectElement(String? id) => _patchReady((s) => s.copyWith(selectedElementId: id));

  void updateElement(String id, EditorElement Function(EditorElement) transform) {
    _patchReady((s) {
      final next = s.elements.map((e) => e.id == id ? transform(e) : e).toList();
      return s.copyWith(elements: next, clearQuote: true);
    });
  }

  void setProductionMethod(String elementId, ProductionMethod method) {
    updateElement(elementId, (e) => e.copyWith(productionMethod: method));
  }

  Future<void> fetchQuote() async {
    final current = state;
    if (current is! DesignEditorReady) return;
    emit(current.copyWith(quoting: true));
    try {
      final body = _buildPayload(current, forQuote: true);
      final quote = await _designs.quote(body);
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(quoting: false, quote: quote));
      }
    } catch (e) {
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(quoting: false, actionError: e.toString()));
      }
    }
  }

  Future<void> saveDesign({String title = 'My Zouq design'}) async {
    final current = state;
    if (current is! DesignEditorReady) return;
    emit(current.copyWith(saving: true, actionError: null));
    try {
      final body = _buildPayload(current, forQuote: false, title: title);
      final saved = await _designs.saveDesign(body, designId: current.savedDesignId);
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(saving: false, savedDesignId: saved.id));
      }
    } catch (e) {
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(saving: false, actionError: e.toString()));
      }
    }
  }

  Future<void> pickAndUploadImage() async {
    final current = state;
    if (current is! DesignEditorReady) return;
    final code = current.activeSurfaceCode;
    if (code == null) return;
    try {
      final file = await _picker.pickImage(source: ImageSource.gallery, maxWidth: 2048);
      if (file == null) return;
      emit(current.copyWith(saving: true, actionError: null));
      final uploaded = await _designs.uploadImage(file.path, file.name);
      final ready = state;
      if (ready is! DesignEditorReady) return;
      final element = EditorElement(
        id: 'upload_${DateTime.now().microsecondsSinceEpoch}',
        surfaceCode: code,
        userUploadId: uploaded.id,
        productionMethod: ProductionMethod.printing,
        normX: 0.2,
        normY: 0.2,
        normWidth: 0.6,
        normHeight: 0.6,
        zIndex: ready.elements.length,
        label: 'Upload',
      );
      emit(ready.copyWith(
        saving: false,
        elements: [...ready.elements, element],
        selectedElementId: element.id,
        clearQuote: true,
      ));
    } catch (e) {
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(saving: false, actionError: e.toString()));
      }
    }
  }

  Future<OrderDto?> createOrder({int quantity = 1}) async {
    final current = state;
    if (current is! DesignEditorReady) return null;
    emit(current.copyWith(saving: true, actionError: null));
    try {
      final body = _buildPayload(current, forQuote: false, title: 'Order design');
      // Always persist latest draft (including derived) before ordering
      final saved = await _designs.saveDesign(body, designId: current.savedDesignId);
      final order = await _orders.createOrder(designId: saved.id, quantity: quantity);
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(saving: false, savedDesignId: saved.id, createdOrderId: order.id));
      }
      return order;
    } catch (e) {
      final ready = state;
      if (ready is DesignEditorReady) {
        emit(ready.copyWith(saving: false, actionError: e.toString()));
      }
      return null;
    }
  }

  Map<String, dynamic> _buildPayload(
    DesignEditorReady state, {
    required bool forQuote,
    String? title,
  }) {
    final elements = <Map<String, dynamic>>[];
    for (final el in state.elements) {
      final surface = state.config.surfaces.firstWhere((s) => s.code == el.surfaceCode);
      final area = surface.designArea;
      if (area == null) continue;
      elements.add(forQuote ? el.toQuoteJson(area) : el.toSaveJson(area));
    }

    final map = <String, dynamic>{
      'product_id': state.config.id,
      if (state.selectedFabricId != null) 'fabric_id': state.selectedFabricId,
      if (state.selectedCutId != null) 'cut_style_id': state.selectedCutId,
      if (state.selectedSizeId != null) 'product_size_id': state.selectedSizeId,
      if (state.selectedPrintingId != null) 'printing_option_id': state.selectedPrintingId,
      'elements': elements,
    };

    if (!forQuote) {
      map['title'] = title ?? 'My Zouq design';
      map['status'] = 'draft';
      map['visibility'] = 'private';
    }

    return map;
  }

  void _patchReady(DesignEditorReady Function(DesignEditorReady) patch) {
    final current = state;
    if (current is DesignEditorReady) {
      emit(patch(current));
    }
  }
}

extension _FirstOrNull<E> on Iterable<E> {
  E? get firstOrNull {
    final it = iterator;
    if (it.moveNext()) return it.current;
    return null;
  }
}
