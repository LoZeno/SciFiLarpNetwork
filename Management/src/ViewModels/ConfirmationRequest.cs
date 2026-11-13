namespace SciFiLarpNetwork.Management.ViewModels;

public sealed record ConfirmationRequest(string Title, string Message, string ConfirmLabel, string CancelLabel);
