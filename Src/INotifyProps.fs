namespace FsEx.Wpf

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Input
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

// TODO .. not implemented yet
module INotifyProps = 
    ()


    (*
    // from https://github.com/tatsuya-midorikawa/FsWpf/blob/main/src/templates/FsWpf.ViewModels/MainWindowViewModel.fs    

    [<AbstractClass>]
    type ViewModel() =
      let propertyChanged = Event<_, _>()
  
      interface INotifyPropertyChanged with
        [<CLIEvent>]
        member __.PropertyChanged = propertyChanged.Publish

      member __.OnPropertyChanged([<CallerMemberName; Optional; DefaultParameterValue("")>] memberName: string) =
        if not(System.String.IsNullOrEmpty(memberName)) then
          propertyChanged.Trigger(__, PropertyChangedEventArgs(memberName))

    type MainWindowViewModel() =
      inherit ViewModel()
      let mutable title = "F# Wpf Sample Text"
  
      member __.Title
        with get() = title
        and set(title') = title <- title'; __.OnPropertyChanged()
    

    
    /// alternative


    
    type ViewModelBase() = //http://www.fssnip.net/4Q/title/F-Quotations-with-INotifyPropertyChanged
        let propertyChanged = new Event<_, _>()
        let toPropName(query : Expr) = 
            match query with
            | PropertyGet(a, b, list) ->
                b.Name
            | _ -> ""

        interface INotifyPropertyChanged with
            [<CLIEvent>]
            member x.PropertyChanged = propertyChanged.Publish

        abstract member OnPropertyChanged: string -> unit
        default x.OnPropertyChanged(propertyName : string) =
            propertyChanged.Trigger(x, new PropertyChangedEventArgs(propertyName))

        member x.OnPropertyChanged(expr : Expr) =
            let propName = toPropName(expr)
            x.OnPropertyChanged(propName)

    type TestModel() =
        inherit ViewModelBase()

        let mutable selectedItem : obj = null

        member x.SelectedItem
            with get() = selectedItem
            and set(v : obj) = 
                selectedItem <- v
                x.OnPropertyChanged(<@ x.SelectedItem @>)

    *)
