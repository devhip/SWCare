// The API's DTOs (same names as web/types). JSON is PascalCase; enums come as strings.

List<T> _list<T>(Object? json, T Function(Map<String, dynamic>) from) =>
    (json as List<dynamic>? ?? const []).map((e) => from(e as Map<String, dynamic>)).toList();

DateTime? _date(Object? value) => value == null ? null : DateTime.parse(value as String);

/// GET api/Auth/Me — roles follow from the department (plan 3).
class Me {
  Me.fromJson(Map<String, dynamic> j)
      : userId = j['UserId'] as int,
        username = j['Username'] as String,
        displayName = j['DisplayName'] as String,
        email = j['Email'] as String?,
        departmentId = j['DepartmentId'] as int,
        departmentName = j['DepartmentName'] as String,
        isAdmin = j['IsAdmin'] as bool,
        isDepartmentHead = j['IsDepartmentHead'] as bool,
        isSupportHead = j['IsSupportHead'] as bool,
        isProgrammer = j['IsProgrammer'] as bool,
        isProgrammerLead = j['IsProgrammerLead'] as bool,
        isExecutive = j['IsExecutive'] as bool? ?? false,
        canViewAllTickets = j['CanViewAllTickets'] as bool? ?? false,
        mustChangePassword = j['MustChangePassword'] as bool,
        language = j['Language'] as String? ?? 'th';

  final int userId;
  final String username;
  final String displayName;
  final String? email;
  final int departmentId;
  final String departmentName;
  final bool isAdmin;
  final bool isDepartmentHead;
  final bool isSupportHead;
  final bool isProgrammer;
  final bool isProgrammerLead;
  final bool isExecutive;
  final bool canViewAllTickets;
  final bool mustChangePassword;
  final String language;

  /// Same as DASHBOARD_ROLES of the web.
  bool get seesDashboard => isExecutive || isAdmin || isSupportHead || isProgrammerLead || isDepartmentHead;
}

class UserBrief {
  UserBrief.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        displayName = j['DisplayName'] as String;

  final int id;
  final String displayName;

  static UserBrief? maybe(Object? json) => json == null ? null : UserBrief.fromJson(json as Map<String, dynamic>);
}

class IdName {
  IdName.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        name = j['Name'] as String;

  final int id;
  final String name;
}

/// Not over yet: what the งานของฉัน tabs show.
const openStatuses = ['PendingApproval', 'Rejected', 'PendingTriage', 'WaitingInfo', 'Queued', 'InProgress', 'Resolved'];
const allStatuses = [...openStatuses, 'Closed', 'Cancelled', 'Declined'];
const ticketTypes = ['Bug', 'FeatureRequest', 'Testing', 'DataService'];
const priorities = ['Immediate', 'Urgent', 'Normal'];

class TicketListItem {
  TicketListItem.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        ticketNo = j['TicketNo'] as String,
        title = j['Title'] as String,
        type = j['Type'] as String,
        productName = j['ProductName'] as String,
        status = j['Status'] as String,
        requestedPriority = j['RequestedPriority'] as String,
        actualPriority = j['ActualPriority'] as String?,
        reporter = UserBrief.fromJson(j['Reporter'] as Map<String, dynamic>),
        assignee = UserBrief.maybe(j['Assignee']),
        customerName = j['CustomerName'] as String?,
        reportedAt = DateTime.parse(j['ReportedAt'] as String),
        plannedDueDate = j['PlannedDueDate'] as String?,
        isOverdue = j['IsOverdue'] as bool? ?? false;

  final int id;
  final String ticketNo;
  final String title;
  final String type;
  final String productName;
  final String status;
  final String requestedPriority;
  final String? actualPriority;
  final UserBrief reporter;
  final UserBrief? assignee;
  final String? customerName;
  final DateTime reportedAt;
  final String? plannedDueDate;
  final bool isOverdue;

  String get priority => actualPriority ?? requestedPriority;
}

class TicketCounts {
  TicketCounts.fromJson(Map<String, dynamic> j)
      : values = {for (final e in j.entries) e.key: (e.value as num).toInt()};

  final Map<String, int> values;

  int operator [](String scope) => values[scope] ?? 0;
}

class Attachment {
  Attachment.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        fileName = j['FileName'] as String,
        contentType = j['ContentType'] as String,
        sizeBytes = (j['SizeBytes'] as num).toInt(),
        uploadedBy = UserBrief.fromJson(j['UploadedBy'] as Map<String, dynamic>),
        createdAt = DateTime.parse(j['CreatedAt'] as String);

  final int id;
  final String fileName;
  final String contentType;
  final int sizeBytes;
  final UserBrief uploadedBy;
  final DateTime createdAt;
}

class TicketComment {
  TicketComment.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        author = UserBrief.fromJson(j['Author'] as Map<String, dynamic>),
        body = j['Body'] as String,
        createdAt = DateTime.parse(j['CreatedAt'] as String),
        attachments = _list(j['Attachments'], Attachment.fromJson);

  final int id;
  final UserBrief author;
  final String body;
  final DateTime createdAt;
  final List<Attachment> attachments;
}

class Activity {
  Activity.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        type = j['Type'] as String,
        fromStatus = j['FromStatus'] as String?,
        toStatus = j['ToStatus'] as String?,
        actor = UserBrief.maybe(j['Actor']),
        note = j['Note'] as String?,
        data = j['Data'] as String?,
        createdAt = DateTime.parse(j['CreatedAt'] as String);

  final int id;
  final String type;
  final String? fromStatus;
  final String? toStatus;
  final UserBrief? actor;
  final String? note;
  final String? data;
  final DateTime createdAt;
}

class TicketDetail {
  TicketDetail.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        ticketNo = j['TicketNo'] as String,
        legacyRef = j['LegacyRef'] as String?,
        title = j['Title'] as String,
        type = j['Type'] as String,
        product = IdName.fromJson(j['Product'] as Map<String, dynamic>),
        version = j['Version'] as String?,
        customerName = j['CustomerName'] as String?,
        description = j['Description'] as String,
        testedDetails = j['TestedDetails'] as String?,
        requestedPriority = j['RequestedPriority'] as String,
        actualPriority = j['ActualPriority'] as String?,
        status = j['Status'] as String,
        reporter = UserBrief.fromJson(j['Reporter'] as Map<String, dynamic>),
        requester = UserBrief.maybe(j['Requester']),
        department = IdName.fromJson(j['Department'] as Map<String, dynamic>),
        approver = UserBrief.maybe(j['Approver']),
        approvedAt = _date(j['ApprovedAt']),
        assignee = UserBrief.maybe(j['Assignee']),
        plannedStartDate = j['PlannedStartDate'] as String?,
        plannedDueDate = j['PlannedDueDate'] as String?,
        reportedAt = DateTime.parse(j['ReportedAt'] as String),
        startedAt = _date(j['StartedAt']),
        resolvedAt = _date(j['ResolvedAt']),
        closedAt = _date(j['ClosedAt']),
        resolution = j['Resolution'] as String?,
        needsLegacyReview = j['NeedsLegacyReview'] as bool? ?? false,
        legacyReporterName = j['LegacyReporterName'] as String?,
        legacyRequesterName = j['LegacyRequesterName'] as String?,
        legacyApproverName = j['LegacyApproverName'] as String?,
        legacyAssigneeName = j['LegacyAssigneeName'] as String?,
        deviceModels = _list(j['DeviceModels'], IdName.fromJson),
        watchers = _list(j['Watchers'], UserBrief.fromJson),
        attachments = _list(j['Attachments'], Attachment.fromJson),
        comments = _list(j['Comments'], TicketComment.fromJson),
        activities = _list(j['Activities'], Activity.fromJson),
        allowedActions = (j['AllowedActions'] as List<dynamic>? ?? const []).cast<String>().toSet();

  final int id;
  final String ticketNo;
  final String? legacyRef;
  final String title;
  final String type;
  final IdName product;
  final String? version;
  final String? customerName;
  final String description;
  final String? testedDetails;
  final String requestedPriority;
  final String? actualPriority;
  final String status;
  final UserBrief reporter;
  final UserBrief? requester;
  final IdName department;
  final UserBrief? approver;
  final DateTime? approvedAt;
  final UserBrief? assignee;
  final String? plannedStartDate;
  final String? plannedDueDate;
  final DateTime reportedAt;
  final DateTime? startedAt;
  final DateTime? resolvedAt;
  final DateTime? closedAt;
  final String? resolution;
  final bool needsLegacyReview;
  final String? legacyReporterName;
  final String? legacyRequesterName;
  final String? legacyApproverName;
  final String? legacyAssigneeName;
  final List<IdName> deviceModels;
  final List<UserBrief> watchers;
  final List<Attachment> attachments;
  final List<TicketComment> comments;
  final List<Activity> activities;
  final Set<String> allowedActions;

  bool can(String action) => allowedActions.contains(action);
}

class BoardCard {
  BoardCard.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        ticketNo = j['TicketNo'] as String,
        title = j['Title'] as String,
        productName = j['ProductName'] as String,
        customerName = j['CustomerName'] as String?,
        status = j['Status'] as String,
        requestedPriority = j['RequestedPriority'] as String,
        actualPriority = j['ActualPriority'] as String?,
        assigneeId = j['AssigneeId'] as int?,
        plannedDueDate = j['PlannedDueDate'] as String?,
        isOverdue = j['IsOverdue'] as bool? ?? false,
        queueOrder = j['QueueOrder'] as int? ?? 0;

  final int id;
  final String ticketNo;
  final String title;
  final String productName;
  final String? customerName;
  final String status;
  final String requestedPriority;
  final String? actualPriority;
  final int? assigneeId;
  final String? plannedDueDate;
  final bool isOverdue;
  final int queueOrder;
}

class Board {
  Board.fromJson(Map<String, dynamic> j)
      : programmers = _list(j['Programmers'], UserBrief.fromJson),
        cards = _list(j['Cards'], BoardCard.fromJson);

  final List<UserBrief> programmers;
  final List<BoardCard> cards;
}

class AppNotification {
  AppNotification.fromJson(Map<String, dynamic> j)
      : id = (j['Id'] as num).toInt(),
        type = j['Type'] as String,
        ticketId = j['TicketId'] as int?,
        actorName = j['ActorName'] as String?,
        data = j['Data'] as String?,
        createdAt = DateTime.parse(j['CreatedAt'] as String),
        isRead = j['IsRead'] as bool;

  final int id;
  final String type;
  final int? ticketId;
  final String? actorName;
  final String? data;
  final DateTime createdAt;
  final bool isRead;
}

class Product {
  Product.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        name = j['Name'] as String,
        ownerUserId = j['OwnerUserId'] as int?,
        isActive = j['IsActive'] as bool;

  final int id;
  final String name;
  final int? ownerUserId;
  final bool isActive;
}

class DeviceModel {
  DeviceModel.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        name = j['Name'] as String,
        isActive = j['IsActive'] as bool;

  final int id;
  final String name;
  final bool isActive;
}

class UserLookup {
  UserLookup.fromJson(Map<String, dynamic> j)
      : id = j['Id'] as int,
        displayName = j['DisplayName'] as String,
        departmentName = j['DepartmentName'] as String? ?? '';

  final int id;
  final String displayName;
  final String departmentName;
}
