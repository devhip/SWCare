import 'package:flutter/material.dart';

/// Placeholder until the next step of the app.
class TicketFormPage extends StatelessWidget {
  const TicketFormPage({super.key, this.editId});

  final int? editId;

  @override
  Widget build(BuildContext context) => Scaffold(appBar: AppBar(), body: const Center(child: Text('TicketFormPage')));
}
