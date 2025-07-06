using Microsoft.CodeAnalysis;

namespace MVVM.VBSourceGenerators.Generators;

internal static class DiagnosticDescriptors
{

    internal static readonly DiagnosticDescriptor AsyncSubWarning = new DiagnosticDescriptor(
        id: "IRI001",
        title: "Async Sub is not supported for RelayCommand",
        messageFormat: "Method '{0}' is an Async Sub. Use Async Function returning Task for async RelayCommands.",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor SyncFunctionWarning = new DiagnosticDescriptor(
        id: "IRI002",
        title: "Synchronous Functions should not be used as RelayCommands",
        messageFormat: "Method '{0}' is a synchronous function. Use a Sub instead as returns will be discarded.",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor TooManyParametersError = new DiagnosticDescriptor(
        id: "IRI003",
        title: "Multiple parameters are not support for RelayCommands",
        messageFormat: "Method '{0}' contains multiple parameters. Only one parameter can be used with a RelayCommand, except when the second parameter is a CancellationToken in an async method",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor CanExecuteParameterDefined = new DiagnosticDescriptor(
        id: "IRI004",
        title: "CanExecute property not implemented for RelayCommand attribute",
        messageFormat: "Method '{0}' describes the 'CanExecute' property which is not supported and will be ignored. You should instead create a method called Can{0}.",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MissingCancellationTokenError = new DiagnosticDescriptor(
        id: "IRI005",
        title: "CancellationToken Required",
        messageFormat: "Method '{0}' cannot be annotated with the <RelayCommand> attribute specifying to include a cancel command, as it does not map to an asynchronous command type taking a cancellation token",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MissingObservableRecipientInheritance = new DiagnosticDescriptor(
        id: "IRI006",
        title: "ObservableRecipient Inheritance Required",
        messageFormat: "Method '{0}' cannot be annotated with the <NotifyPropertyChangedRecipients> attribute as the class does not inherit from ObservableRecipient",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


}
