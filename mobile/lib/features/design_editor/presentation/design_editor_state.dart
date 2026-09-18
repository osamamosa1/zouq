part of 'design_editor_cubit.dart';

sealed class DesignEditorState extends Equatable {
  const DesignEditorState();

  @override
  List<Object?> get props => [];
}

final class DesignEditorInitial extends DesignEditorState {
  const DesignEditorInitial();
}

final class DesignEditorLoading extends DesignEditorState {
  const DesignEditorLoading();
}

final class DesignEditorError extends DesignEditorState {
  const DesignEditorError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}

final class DesignEditorReady extends DesignEditorState {
  const DesignEditorReady({
    required this.config,
    this.selectedFabricId,
    this.selectedCutId,
    this.selectedSizeId,
    this.selectedPrintingId,
    this.activeSurfaceCode,
    required this.elements,
    this.selectedElementId,
    this.quote,
    this.quoting = false,
    this.saving = false,
    this.savedDesignId,
    this.createdOrderId,
    this.actionError,
  });

  final ProductConfigDto config;
  final String? selectedFabricId;
  final String? selectedCutId;
  final String? selectedSizeId;
  final String? selectedPrintingId;
  final String? activeSurfaceCode;
  final List<EditorElement> elements;
  final String? selectedElementId;
  final PriceBreakdownDto? quote;
  final bool quoting;
  final bool saving;
  final String? savedDesignId;
  final String? createdOrderId;
  final String? actionError;

  EditorElement? get selectedElement {
    if (selectedElementId == null) return null;
    for (final e in elements) {
      if (e.id == selectedElementId) return e;
    }
    return null;
  }

  SurfaceDto? get activeSurface {
    if (activeSurfaceCode == null) return null;
    for (final s in config.surfaces) {
      if (s.code == activeSurfaceCode) return s;
    }
    return null;
  }

  DesignEditorReady copyWith({
    String? selectedFabricId,
    String? selectedCutId,
    String? selectedSizeId,
    String? selectedPrintingId,
    String? activeSurfaceCode,
    List<EditorElement>? elements,
    String? selectedElementId,
    bool clearSelectedElement = false,
    PriceBreakdownDto? quote,
    bool? quoting,
    bool? saving,
    String? savedDesignId,
    String? createdOrderId,
    String? actionError,
    bool clearQuote = false,
  }) {
    return DesignEditorReady(
      config: config,
      selectedFabricId: selectedFabricId ?? this.selectedFabricId,
      selectedCutId: selectedCutId ?? this.selectedCutId,
      selectedSizeId: selectedSizeId ?? this.selectedSizeId,
      selectedPrintingId: selectedPrintingId ?? this.selectedPrintingId,
      activeSurfaceCode: activeSurfaceCode ?? this.activeSurfaceCode,
      elements: elements ?? this.elements,
      selectedElementId: clearSelectedElement
          ? selectedElementId
          : (selectedElementId ?? this.selectedElementId),
      quote: clearQuote ? null : (quote ?? this.quote),
      quoting: quoting ?? this.quoting,
      saving: saving ?? this.saving,
      savedDesignId: savedDesignId ?? this.savedDesignId,
      createdOrderId: createdOrderId ?? this.createdOrderId,
      actionError: actionError,
    );
  }

  @override
  List<Object?> get props => [
        config.id,
        selectedFabricId,
        selectedCutId,
        selectedSizeId,
        selectedPrintingId,
        activeSurfaceCode,
        elements,
        selectedElementId,
        quote?.total,
        quoting,
        saving,
        savedDesignId,
        createdOrderId,
        actionError,
      ];
}
