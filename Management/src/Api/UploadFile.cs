namespace SciFiLarpNetwork.Management.Api;

/// <param name="FieldName">The action field the file is sent as.</param>
/// <param name="FileName">The file's own name; the server never uses it to decide the type.</param>
public sealed record UploadFile(string FieldName, string FileName, byte[] Content);
