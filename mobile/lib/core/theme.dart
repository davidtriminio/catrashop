import 'package:flutter/material.dart';

/// Design tokens shared with the web apps (frontend/shared/theme.scss): neutral surfaces, near-black primary and one
/// lime accent reserved for "needs attention" and highlights.
class AppColors {
  static const ink = Color(0xFF0B0B0C);
  static const lime = Color(0xFFC8F169);
  static const limeInk = Color(0xFF1C2600);
  static const bg = Color(0xFFF4F4F5);
  static const surface = Colors.white;
  static const border = Color(0xFFE5E5E8);
  static const borderStrong = Color(0xFFD4D4D8);
  static const muted = Color(0xFF6B6B73);
  static const danger = Color(0xFFB91C1C);

  // Dark scheme
  static const darkBg = Color(0xFF0B0B0C);
  static const darkSurface = Color(0xFF18181B);
  static const darkBorder = Color(0xFF2A2A2E);
  static const darkMuted = Color(0xFFA1A1AA);
}

/// Spacing scale (4 px base), mirroring `--space-*` on the web.
class Gap {
  static const s1 = 4.0, s2 = 8.0, s3 = 12.0, s4 = 16.0, s5 = 24.0, s6 = 32.0;
}

class AppTheme {
  static const radius = 14.0;

  static ThemeData light() => _build(
        brightness: Brightness.light,
        scheme: const ColorScheme.light(
          primary: AppColors.ink,
          onPrimary: Colors.white,
          secondary: AppColors.lime,
          onSecondary: AppColors.limeInk,
          surface: AppColors.surface,
          onSurface: AppColors.ink,
          onSurfaceVariant: AppColors.muted,
          outline: AppColors.borderStrong,
          outlineVariant: AppColors.border,
          error: AppColors.danger,
        ),
        scaffold: AppColors.bg,
        border: AppColors.border,
      );

  static ThemeData dark() => _build(
        brightness: Brightness.dark,
        scheme: const ColorScheme.dark(
          primary: Colors.white,
          onPrimary: AppColors.ink,
          secondary: AppColors.lime,
          onSecondary: AppColors.limeInk,
          surface: AppColors.darkSurface,
          onSurface: Colors.white,
          onSurfaceVariant: AppColors.darkMuted,
          outline: Color(0xFF3F3F46),
          outlineVariant: AppColors.darkBorder,
          error: Color(0xFFF87171),
        ),
        scaffold: AppColors.darkBg,
        border: AppColors.darkBorder,
      );

  static ThemeData _build({
    required Brightness brightness,
    required ColorScheme scheme,
    required Color scaffold,
    required Color border,
  }) {
    final base = ThemeData(useMaterial3: true, brightness: brightness, colorScheme: scheme);
    final text = base.textTheme.apply(bodyColor: scheme.onSurface, displayColor: scheme.onSurface).copyWith(
          headlineSmall: base.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700, letterSpacing: -0.4),
          titleLarge: base.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700, letterSpacing: -0.3),
          titleMedium: base.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
          labelLarge: base.textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w600),
        );
    const pill = StadiumBorder();

    return base.copyWith(
      scaffoldBackgroundColor: scaffold,
      textTheme: text,
      appBarTheme: AppBarTheme(
        backgroundColor: scaffold,
        foregroundColor: scheme.onSurface,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: text.titleLarge,
      ),
      cardTheme: CardThemeData(
        color: scheme.surface,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(radius),
          side: BorderSide(color: border),
        ),
      ),
      dividerTheme: DividerThemeData(color: border, space: 1, thickness: 1),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size(0, 50),
          shape: pill,
          textStyle: text.labelLarge,
          padding: const EdgeInsets.symmetric(horizontal: Gap.s5),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(0, 50),
          shape: pill,
          side: BorderSide(color: scheme.outline),
          textStyle: text.labelLarge,
          padding: const EdgeInsets.symmetric(horizontal: Gap.s5),
        ),
      ),
      textButtonTheme: TextButtonThemeData(style: TextButton.styleFrom(shape: pill, textStyle: text.labelLarge)),
      chipTheme: ChipThemeData(
        shape: pill,
        side: BorderSide(color: scheme.outline),
        backgroundColor: scheme.surface,
        selectedColor: scheme.primary,
        labelStyle: text.labelLarge?.copyWith(fontWeight: FontWeight.w500),
        secondaryLabelStyle: text.labelLarge?.copyWith(color: scheme.onPrimary),
        checkmarkColor: scheme.onPrimary,
        padding: const EdgeInsets.symmetric(horizontal: Gap.s2),
        showCheckmark: false,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: scheme.surface,
        border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide(color: scheme.outline)),
        enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide(color: scheme.outline)),
        focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide(color: scheme.primary, width: 1.6)),
        contentPadding: const EdgeInsets.symmetric(horizontal: Gap.s4, vertical: Gap.s4),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: scheme.surface,
        indicatorColor: scheme.secondary,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        height: 68,
        labelTextStyle: WidgetStatePropertyAll(text.labelMedium?.copyWith(fontWeight: FontWeight.w600)),
        iconTheme: WidgetStateProperty.resolveWith((states) => IconThemeData(
              color: states.contains(WidgetState.selected) ? AppColors.limeInk : scheme.onSurfaceVariant,
            )),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(24))),
        showDragHandle: true,
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      ),
    );
  }
}
