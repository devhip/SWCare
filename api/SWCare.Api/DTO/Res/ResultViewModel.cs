namespace SWCare.Api.DTO.Res;

/// <summary>Envelope of every response, as in EasyPark / Easy Next: { Success, Message, Result, Code }.</summary>
public sealed class ResultViewModel<T>
{
    public bool Success { get; set; }
    public T? Result { get; set; }
    public string Message { get; set; } = "";
    public string? Code { get; set; }
}

/// <summary>Envelope of a list, with PageInfo when the list is paged.</summary>
public sealed class ResultsViewModel<T>
{
    public bool Success { get; set; }
    public List<T>? Result { get; set; }
    public string Message { get; set; } = "";
    public string? Code { get; set; }
    public PageInfoViewModel? PageInfo { get; set; }
}

public sealed class PageInfoViewModel(int totalRecords, int pageNumber, int pageLimit)
{
    public int PageNumber { get; } = pageNumber;
    public int PageLimit { get; } = pageLimit;
    public int TotalPages { get; } = pageLimit <= 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageLimit);
    public int TotalRecords { get; } = totalRecords;
}

/// <summary>One page of a list, as a control returns it; the controller wraps it with ApiResult.OkPage.</summary>
public sealed record PagedRes<T>(List<T> Items, int TotalRecords);

public static class ApiResult
{
    public static ResultViewModel<T> Ok<T>(T result, string message = "OK") =>
        new() { Success = true, Result = result, Message = message };

    public static ResultsViewModel<T> OkList<T>(List<T> result, PageInfoViewModel? page = null, string message = "OK") =>
        new() { Success = true, Result = result, Message = message, PageInfo = page };

    public static ResultsViewModel<T> OkPage<T>(PagedRes<T> page, DTO.Req.PageReq request) =>
        OkList(page.Items, new PageInfoViewModel(page.TotalRecords, request.PageNumber, request.PageLimit));

    public static ResultViewModel<T> Fail<T>(string message, string code) =>
        new() { Success = false, Message = message, Code = code };
}
