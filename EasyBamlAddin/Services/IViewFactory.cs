namespace EasyBamlAddin.Services
{
    public interface IViewFactory
    {
        TViewModel CreateViewModel<TViewModel>();
    }
}
