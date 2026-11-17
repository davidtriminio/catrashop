import 'dart:math' as math;

import 'package:flutter/widgets.dart';

/// Bottom padding a bottom sheet needs so its buttons stay above the system UI: the keyboard while it is open,
/// otherwise the Android navigation bar (3-button or gesture bar) or the iOS home indicator. Android 15+ draws apps
/// edge to edge, so without this the sheet's last row ends up underneath those buttons.
double sheetBottomInset(BuildContext context) {
  final media = MediaQuery.of(context);
  return math.max(media.viewInsets.bottom, media.viewPadding.bottom);
}
