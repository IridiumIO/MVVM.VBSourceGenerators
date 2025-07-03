# MVVM.VBSourceGenerators

The [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet) source generators only work in C#. This package augments the toolkit to allow some of the generators to work for VB.NET.



## Working

- `<ObservableProperty>` attribute generates a public property with change notification.
- `<NotifyPropertyChangedFor(NameOf(T))>` attribute generates a property with change notification for existing properties.
- `<RelayCommand>`
   - Can decorate `Sub`, `Async Sub`, or `Async Function`
   - Supports `CanExecute` callback functionality
   - Supports passing a parameter to the command


## Planned
- `<RelayCommand>`
   - Passing cancellation tokens
- `NotifyCanExecuteChangedFor(NameOf(T))`


## Installation

1. Add the NuGet package for MVVM.VBSourceGenerators to your VB.NET project:
    ```shell
    dotnet add package IridiumIO.MVVM.VBSourceGenerators
    ```
2. Ensure that you also reference `CommunityToolkit.MVVM` in your project.

## ObservableProperty Usage

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


## RelayCommand Usage

1. In your VB.NET ViewModel, use the `[RelayCommand]` attribute as you would in C# on a method.


#### Examples:

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class MyViewModel
    Inherits ObservableObject

    <RelayCommand>
    Private Sub Save()
        ' Save logic here
    End Sub

    <RelayCommand>
    Private Sub SelectItem(item As String)
        ' Logic to select an item
    End Sub

    <RelayCommand>
    Private Async Function LoadDataAsync() As Task
        ' Load data logic here
        Await Task.Delay(1000) ' Simulating async work
    End Function

End Class
```

&nbsp;

#### Example 2: Including CanExecute callback

By creating a function that returns a `Boolean` and prefixing it with `Can`, you can control whether the command can execute.

```vbnet
Imports CommunityToolkit.Mvvm.ComponentModel

Partial Public Class PersonViewModel
    Inherits ObservableObject

    <RelayCommand>
    Private Sub SaveData()
        ' Logic to save data
    End Sub

    Private Function CanSaveData() As Boolean
        ' Logic to determine if saving is allowed
         Return MyData.Count > 0
    End Function

End Class
```
