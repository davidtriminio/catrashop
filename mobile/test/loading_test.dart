import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:catrashop_admin/core/loading.dart';

Widget _host(Widget child, {bool disableAnimations = false}) => MaterialApp(
      home: MediaQuery(
        data: MediaQueryData(disableAnimations: disableAnimations),
        child: Scaffold(body: Center(child: child)),
      ),
    );

void main() {
  group('BusyButton', () {
    testWidgets('keeps its size while busy and hides the label behind a spinner', (tester) async {
      var taps = 0;
      Widget button(bool busy) => _host(SizedBox(width: 240, child: BusyButton(label: 'Aprobar', busy: busy, onPressed: () => taps++)));

      await tester.pumpWidget(button(false));
      final idle = tester.getSize(find.byType(FilledButton));
      expect(find.byType(CircularProgressIndicator), findsNothing);

      await tester.pumpWidget(button(true));
      expect(tester.getSize(find.byType(FilledButton)), idle);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(tester.widget<Opacity>(find.ancestor(of: find.text('Aprobar'), matching: find.byType(Opacity)).first).opacity, 0);
    });

    testWidgets('ignores taps while busy but still reacts when idle', (tester) async {
      var taps = 0;
      Widget button(bool busy) => _host(BusyButton(label: 'Guardar', busy: busy, onPressed: () => taps++));

      await tester.pumpWidget(button(true));
      await tester.tap(find.byType(FilledButton));
      expect(taps, 0);

      await tester.pumpWidget(button(false));
      await tester.tap(find.byType(FilledButton));
      expect(taps, 1);
    });

    testWidgets('stays tappable-looking while busy (never the disabled grey)', (tester) async {
      await tester.pumpWidget(_host(BusyButton(label: 'Ir', busy: true, onPressed: () {})));
      expect(tester.widget<FilledButton>(find.byType(FilledButton)).onPressed, isNotNull);
    });
  });

  group('Skeletons', () {
    testWidgets('shimmer animates, and an announcement is exposed once', (tester) async {
      await tester.pumpWidget(_host(const SkeletonScreen(child: Column(children: [SkeletonBox(width: 100), SkeletonBox(width: 60)]))));
      expect(find.bySemanticsLabel('Cargando'), findsOneWidget);
      // the repeating animation keeps scheduling frames, so advance by hand instead of settling
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.byType(SkeletonBox), findsNWidgets(2));
    });

    testWidgets('boxes stay still when the system asks to remove animations', (tester) async {
      await tester.pumpWidget(_host(const SkeletonScreen(child: SkeletonBox(width: 100)), disableAnimations: true));
      await tester.pumpAndSettle(); // would time out if something kept animating
      expect(find.byType(SkeletonBox), findsOneWidget);
    });

    testWidgets('the screen placeholders build without overflowing a phone screen', (tester) async {
      tester.view.physicalSize = const Size(360, 740);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);
      for (final screen in const <Widget>[OrderDetailSkeleton(), SummarySkeleton(), ReceiptSkeleton()]) {
        await tester.pumpWidget(_host(screen));
        await tester.pump(const Duration(milliseconds: 100));
        expect(tester.takeException(), isNull);
      }
    });
  });
}
