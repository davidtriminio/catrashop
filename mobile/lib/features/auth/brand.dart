import 'package:flutter/material.dart';

import '../../core/theme.dart';

/// Wordmark shared by the login and lock screens (same mark as the web header).
class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.size = 28});
  final double size;

  @override
  Widget build(BuildContext context) {
    final style = TextStyle(fontSize: size, fontWeight: FontWeight.w800, letterSpacing: -1, color: Theme.of(context).colorScheme.onSurface);
    return Row(mainAxisSize: MainAxisSize.min, children: [
      Text('Commerce', style: style),
      const SizedBox(width: 2),
      Container(
        padding: EdgeInsets.symmetric(horizontal: size * 0.2),
        decoration: BoxDecoration(color: AppColors.lime, borderRadius: BorderRadius.circular(size * 0.22)),
        child: Text('Hub', style: style.copyWith(color: AppColors.limeInk)),
      ),
    ]);
  }
}
