import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/api.dart';
import '../../models/models.dart';

/// Systems and device models for the forms (active ones only).
final productsProvider = FutureProvider<List<Product>>((ref) async =>
    (await ref.watch(apiProvider).products()).where((p) => p.isActive).toList());

final deviceModelsProvider = FutureProvider<List<DeviceModel>>((ref) async =>
    (await ref.watch(apiProvider).deviceModels()).where((m) => m.isActive).toList());

/// Who can be the assignee (members of the Software department).
final programmersProvider = FutureProvider<List<UserLookup>>((ref) => ref.watch(apiProvider).programmers());
