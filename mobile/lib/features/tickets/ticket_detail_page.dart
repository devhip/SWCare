import 'package:flutter/material.dart';

/// Placeholder until the next step of the app.
class TicketDetailPage extends StatelessWidget {
  const TicketDetailPage({super.key, required this.id});

  final int id;

  @override
  Widget build(BuildContext context) => Scaffold(appBar: AppBar(), body: const Center(child: Text('TicketDetailPage')));
}
