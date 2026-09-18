import 'package:flutter/material.dart';
import 'package:zouq/features/catalog/data/catalog_dtos.dart';
import 'package:zouq/features/design_editor/domain/editor_element.dart';
import 'package:zouq/features/design_editor/domain/normalized_coordinates.dart';

class DesignCanvas extends StatelessWidget {
  const DesignCanvas({
    super.key,
    required this.surface,
    required this.elements,
    required this.selectedElementId,
    required this.onSelect,
    required this.onMoveElement,
    required this.onResizeElement,
    required this.onRotateElement,
  });

  final SurfaceDto surface;
  final List<EditorElement> elements;
  final String? selectedElementId;
  final ValueChanged<String> onSelect;
  final void Function(String id, double normX, double normY) onMoveElement;
  final void Function(String id, double normWidth, double normHeight) onResizeElement;
  final void Function(String id, double rotationDegrees) onRotateElement;

  @override
  Widget build(BuildContext context) {
    final area = surface.designArea;
    if (area == null) {
      return const AspectRatio(
        aspectRatio: 1,
        child: Center(child: Text('No design area configured')),
      );
    }

    final surfaceElements = elements.where((e) => e.surfaceCode == surface.code).toList()
      ..sort((a, b) => a.zIndex.compareTo(b.zIndex));

    return AspectRatio(
      aspectRatio: area.realWidth / area.realHeight,
      child: LayoutBuilder(
        builder: (context, constraints) {
          final designPx = Size(constraints.maxWidth, constraints.maxHeight);
          return DecoratedBox(
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surfaceContainerHighest,
              border: Border.all(color: Theme.of(context).colorScheme.outlineVariant),
            ),
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                if (surface.previewImageUrl != null)
                  Positioned.fill(
                    child: Opacity(
                      opacity: 0.35,
                      child: Image.network(
                        surface.previewImageUrl!,
                        fit: BoxFit.cover,
                        errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                      ),
                    ),
                  ),
                Positioned(
                  left: area.normX * designPx.width,
                  top: area.normY * designPx.height,
                  width: area.normWidth * designPx.width,
                  height: area.normHeight * designPx.height,
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      border: Border.all(
                        color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.5),
                      ),
                    ),
                    child: Stack(
                      clipBehavior: Clip.none,
                      children: [
                        for (final el in surfaceElements)
                          _ElementLayer(
                            key: ValueKey(el.id),
                            element: el,
                            designArea: area,
                            designSize: Size(
                              area.normWidth * designPx.width,
                              area.normHeight * designPx.height,
                            ),
                            selected: el.id == selectedElementId,
                            onSelect: () => onSelect(el.id),
                            onDrag: (dx, dy) {
                              final w = area.normWidth * designPx.width;
                              final h = area.normHeight * designPx.height;
                              final nx = (el.normX + dx / w).clamp(0.0, 1.0 - el.normWidth);
                              final ny = (el.normY + dy / h).clamp(0.0, 1.0 - el.normHeight);
                              onMoveElement(el.id, nx, ny);
                            },
                            onResize: (dw, dh) {
                              final w = area.normWidth * designPx.width;
                              final h = area.normHeight * designPx.height;
                              var nw = (el.normWidth + dw / w).clamp(0.08, 1.0 - el.normX);
                              var nh = (el.normHeight + dh / h).clamp(0.08, 1.0 - el.normY);
                              // Preserve aspect when both change proportionally
                              final aspect = el.normWidth / el.normHeight;
                              if ((dw.abs() > dh.abs())) {
                                nh = (nw / aspect).clamp(0.08, 1.0 - el.normY);
                              } else {
                                nw = (nh * aspect).clamp(0.08, 1.0 - el.normX);
                              }
                              onResizeElement(el.id, nw, nh);
                            },
                            onRotate: (delta) {
                              onRotateElement(el.id, (el.rotationDegrees + delta) % 360);
                            },
                          ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _ElementLayer extends StatelessWidget {
  const _ElementLayer({
    super.key,
    required this.element,
    required this.designArea,
    required this.designSize,
    required this.selected,
    required this.onSelect,
    required this.onDrag,
    required this.onResize,
    required this.onRotate,
  });

  final EditorElement element;
  final DesignAreaDto designArea;
  final Size designSize;
  final bool selected;
  final VoidCallback onSelect;
  final void Function(double dx, double dy) onDrag;
  final void Function(double dw, double dh) onResize;
  final void Function(double deltaDegrees) onRotate;

  @override
  Widget build(BuildContext context) {
    final left = element.normX * designSize.width;
    final top = element.normY * designSize.height;
    final width = element.normWidth * designSize.width;
    final height = element.normHeight * designSize.height;
    final real = NormalizedCoordinates.realDimensions(
      designArea: designArea,
      normWidth: element.normWidth,
      normHeight: element.normHeight,
    );

    return Positioned(
      left: left,
      top: top,
      width: width,
      height: height,
      child: GestureDetector(
        onTap: onSelect,
        onPanUpdate: (d) => onDrag(d.delta.dx, d.delta.dy),
        child: Transform.rotate(
          angle: element.rotationDegrees * 3.1415926535 / 180,
          child: Stack(
            clipBehavior: Clip.none,
            children: [
              DecoratedBox(
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.secondaryContainer.withValues(alpha: 0.85),
                  border: Border.all(
                    color: selected
                        ? Theme.of(context).colorScheme.primary
                        : Theme.of(context).colorScheme.outline,
                    width: selected ? 2 : 1,
                  ),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(4),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        element.label ?? 'Element',
                        style: Theme.of(context).textTheme.labelMedium,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      Text(
                        element.productionMethod.apiValue,
                        style: Theme.of(context).textTheme.labelSmall,
                      ),
                      const Spacer(),
                      Text(
                        '${real.realWidth.toStringAsFixed(2)} × ${real.realHeight.toStringAsFixed(2)}',
                        style: Theme.of(context).textTheme.labelSmall,
                      ),
                    ],
                  ),
                ),
              ),
              if (selected) ...[
                Positioned(
                  right: -8,
                  bottom: -8,
                  child: GestureDetector(
                    onPanUpdate: (d) => onResize(d.delta.dx, d.delta.dy),
                    child: Container(
                      width: 20,
                      height: 20,
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.primary,
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.open_in_full, size: 12, color: Colors.white),
                    ),
                  ),
                ),
                Positioned(
                  left: width / 2 - 12,
                  top: -28,
                  child: GestureDetector(
                    onPanUpdate: (d) => onRotate(d.delta.dx),
                    child: Container(
                      width: 24,
                      height: 24,
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.tertiary,
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(Icons.rotate_right, size: 14, color: Colors.white),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
