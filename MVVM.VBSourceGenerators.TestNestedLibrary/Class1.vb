Imports CommunityToolkit.Mvvm.ComponentModel

'Still works in VB.NET? I'm not arguing
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MVVM.VBSourceGenerators.TestWPF")>

Friend Class Class1 : Inherits ObservableObject

    <ObservableProperty>
    Private _TestProperty As String

End Class


Friend Class Class2 : Inherits ObservableObject

    <ObservableProperty>
    Private _TestProperty As String

End Class
