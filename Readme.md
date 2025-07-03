# MVVM.VBSourceGenerators

The [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet) source generators only work in C#. This package augments the toolkit to allow some of the generators to work for VB.NET.



## Working

- `<ObservableProperty>` attribute generates a public property with change notification.
- `<NotifyPropertyChangedFor(NameOf(T))>` attribute generates a property with change notification for existing properties.

## Planned
- `<RelayCommand>`
- `NotifyCanExecuteChangedFor(NameOf(T))`


### Installation

1. Add the NuGet package for MVVM.VBSourceGenerators to your VB.NET project:
    ```shell
    dotnet add package IridiumIO.MVVM.VBSourceGenerators
    ```
2. Ensure that you also reference `CommunityToolkit.MVVM` in your project.

### Usage

1. In your VB.NET ViewModel, use the `[ObservableProperty]` attribute as you would in C#
2. Make sure your fields follow the naming convention of starting with an underscore (e.g., `_name`).
3. You can then use the generated Uppercase property `Name` in your code.

Example 1:

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class MyViewModel
    Inherits ObservableObject

    <ObservableProperty>
    Private _name As String
End Class
```

&nbsp;

Example 2: Including `NotifyPropertyChangedFor`

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class PersonViewModel
    Inherits ObservableObject

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _firstName As String

    <ObservableProperty>
    <NotifyPropertyChangedFor(NameOf(FullName))>
    Private _lastName As String

    Public ReadOnly Property FullName As String
        Get
            Return $"{FirstName} {LastName}"
        End Get
    End Property

End Class
```

