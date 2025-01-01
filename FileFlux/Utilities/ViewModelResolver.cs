
namespace FileFlux.Utilities
{
    public class ViewModelResolver : IMarkupExtension
    {
        public Type ViewModelType { get; set; }
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var viewModel = serviceProvider.GetService(ViewModelType);
            return viewModel;
        }
    }
}
