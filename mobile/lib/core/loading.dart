import 'package:flutter/material.dart';

import 'theme.dart';

/// Moving highlight shared by every [SkeletonBox] below it, so a whole placeholder screen shimmers in step. With the
/// system's "remove animations" setting on, the boxes stay still.
class Shimmer extends StatefulWidget {
  const Shimmer({super.key, required this.child});
  final Widget child;

  @override
  State<Shimmer> createState() => _ShimmerState();
}

class _ShimmerState extends State<Shimmer> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(vsync: this, duration: const Duration(milliseconds: 1500));
  bool? _running;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final animate = !MediaQuery.disableAnimationsOf(context);
    if (animate == _running) return;
    _running = animate;
    animate ? _controller.repeat() : _controller.stop();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _ShimmerScope(animation: _controller, child: widget.child);
}

class _ShimmerScope extends InheritedWidget {
  const _ShimmerScope({required this.animation, required super.child});
  final Animation<double> animation;

  static Animation<double>? maybeOf(BuildContext context) => context.dependOnInheritedWidgetOfExactType<_ShimmerScope>()?.animation;

  @override
  bool updateShouldNotify(_ShimmerScope old) => animation != old.animation;
}

class _Slide extends GradientTransform {
  const _Slide(this.percent);
  final double percent;

  @override
  Matrix4? transform(Rect bounds, {TextDirection? textDirection}) => Matrix4.translationValues(bounds.width * percent, 0, 0);
}

/// A grey rounded block that stands in for text, an image or a button while content loads.
class SkeletonBox extends StatelessWidget {
  const SkeletonBox({super.key, this.width, this.height = 14, this.radius = 6, this.circle = false});
  final double? width;
  final double height;
  final double radius;
  final bool circle;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final base = scheme.outlineVariant;
    final highlight = Color.lerp(base, scheme.surface, 0.6)!;
    final animation = _ShimmerScope.maybeOf(context);

    BoxDecoration decoration(double t) => BoxDecoration(
          shape: circle ? BoxShape.circle : BoxShape.rectangle,
          borderRadius: circle ? null : BorderRadius.circular(radius),
          gradient: animation == null
              ? null
              : LinearGradient(
                  colors: [base, highlight, base],
                  stops: const [0.1, 0.5, 0.9],
                  begin: const Alignment(-1, -0.2),
                  end: const Alignment(1, 0.2),
                  transform: _Slide(-1 + 2 * t),
                ),
          color: animation == null ? base : null,
        );

    final box = animation == null
        ? Container(width: width, height: circle ? width ?? height : height, decoration: decoration(0))
        : AnimatedBuilder(
            animation: animation,
            builder: (_, _) => Container(width: width, height: circle ? width ?? height : height, decoration: decoration(animation.value)),
          );
    // Decorative: a screen reader should hear the screen's own "loading" label, not a row of empty boxes.
    return ExcludeSemantics(child: box);
  }
}

/// Wraps a placeholder screen: shimmer, one "Cargando…" announcement for screen readers, and nothing tappable.
class SkeletonScreen extends StatelessWidget {
  const SkeletonScreen({super.key, required this.child});
  final Widget child;

  @override
  Widget build(BuildContext context) => Semantics(
        label: 'Cargando',
        liveRegion: true,
        child: IgnorePointer(child: Shimmer(child: child)),
      );
}

/// Placeholder shaped like the order detail screen (header, items, totals).
class OrderDetailSkeleton extends StatelessWidget {
  const OrderDetailSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    Widget card(List<Widget> children) => Card(
          child: Padding(padding: const EdgeInsets.all(Gap.s4), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: children)),
        );
    return SkeletonScreen(
      child: ListView(
        physics: const NeverScrollableScrollPhysics(),
        padding: const EdgeInsets.all(Gap.s4),
        children: [
          card(const [
            SkeletonBox(width: 150, height: 18),
            SizedBox(height: Gap.s3),
            SkeletonBox(width: 90, height: 24, radius: 12),
            SizedBox(height: Gap.s3),
            SkeletonBox(width: 200, height: 12),
          ]),
          const SizedBox(height: Gap.s3),
          card(const [
            SkeletonBox(width: 110, height: 16),
            SizedBox(height: Gap.s4),
            _ItemRow(),
            SizedBox(height: Gap.s3),
            _ItemRow(),
            SizedBox(height: Gap.s3),
            _ItemRow(),
          ]),
          const SizedBox(height: Gap.s3),
          card(const [
            SkeletonBox(width: 90, height: 16),
            SizedBox(height: Gap.s3),
            SkeletonBox(height: 12),
            SizedBox(height: Gap.s2),
            SkeletonBox(width: 160, height: 12),
          ]),
        ],
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  const _ItemRow();

  @override
  Widget build(BuildContext context) => const Row(children: [
        SkeletonBox(width: 40, height: 40, radius: 8),
        SizedBox(width: Gap.s3),
        Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [SkeletonBox(width: 140, height: 13), SizedBox(height: 6), SkeletonBox(width: 80, height: 11)])),
        SkeletonBox(width: 56, height: 14),
      ]);
}

/// Placeholder shaped like the summary screen (attention card and four counters).
class SummarySkeleton extends StatelessWidget {
  const SummarySkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    Widget stat() => const Expanded(
          child: Card(
            child: Padding(
              padding: EdgeInsets.all(Gap.s4),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                SkeletonBox(width: 28, height: 28, circle: true),
                SizedBox(height: Gap.s3),
                SkeletonBox(width: 40, height: 22),
                SizedBox(height: Gap.s2),
                SkeletonBox(width: 70, height: 12),
              ]),
            ),
          ),
        );
    return SkeletonScreen(
      child: ListView(
        physics: const NeverScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(Gap.s4, Gap.s2, Gap.s4, Gap.s5),
        children: [
          const Card(
            child: Padding(
              padding: EdgeInsets.all(Gap.s5),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                SkeletonBox(width: 60, height: 34),
                SizedBox(height: Gap.s3),
                SkeletonBox(width: 180, height: 13),
              ]),
            ),
          ),
          const SizedBox(height: Gap.s3),
          Row(children: [stat(), const SizedBox(width: Gap.s3), stat()]),
          const SizedBox(height: Gap.s3),
          Row(children: [stat(), const SizedBox(width: Gap.s3), stat()]),
        ],
      ),
    );
  }
}

/// Placeholder for the receipt viewer: a tall rounded page where the image will appear.
class ReceiptSkeleton extends StatelessWidget {
  const ReceiptSkeleton({super.key});

  @override
  Widget build(BuildContext context) => const SkeletonScreen(
        child: Padding(
          padding: EdgeInsets.all(Gap.s5),
          child: Center(child: SkeletonBox(width: 260, height: 420, radius: 16)),
        ),
      );
}

/// A button that keeps its size and label space while it works: the label hides and a small spinner turns in its
/// place. It stays enabled-looking but ignores taps, so it never flashes grey. Use [icon] for the leading icon.
class BusyButton extends StatelessWidget {
  const BusyButton({super.key, required this.label, required this.onPressed, this.busy = false, this.icon});
  final String label;
  final VoidCallback? onPressed;
  final bool busy;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final content = Builder(builder: (context) {
      final color = DefaultTextStyle.of(context).style.color;
      return Stack(alignment: Alignment.center, children: [
        Opacity(
          opacity: busy ? 0 : 1,
          child: Row(mainAxisSize: MainAxisSize.min, mainAxisAlignment: MainAxisAlignment.center, children: [
            if (icon != null) ...[Icon(icon, size: 20), const SizedBox(width: 8)],
            Flexible(child: Text(label, overflow: TextOverflow.ellipsis)),
          ]),
        ),
        if (busy) SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: color)),
      ]);
    });
    return Semantics(
      button: true,
      label: busy ? '$label, cargando' : label,
      excludeSemantics: true,
      child: FilledButton(onPressed: onPressed == null ? null : (busy ? () {} : onPressed), child: content),
    );
  }
}
