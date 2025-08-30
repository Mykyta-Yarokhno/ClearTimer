using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.Handlers;

namespace ClearTimer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit().ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // Цей код налаштовує ImageButton для Android
                ImageButtonHandler.Mapper.AppendToMapping("RemoveRippleEffect", (handler, view) =>
                {
                    if (handler.PlatformView is Android.Widget.ImageView imageView)
                    {
                        // Встановлення кольору пульсації на прозорий
                        imageView.SetBackgroundResource(Android.Resource.Color.Transparent);

                        // Також можна спробувати це, щоб повністю видалити ефект
                        // Android.Graphics.Drawables.Drawable originalBackground = imageView.Background;
                        // if (originalBackground != null)
                        // {
                        //     originalBackground.SetColorFilter(new PorterDuffColorFilter(Android.Graphics.Color.Transparent, PorterDuff.Mode.Clear));
                        // }
                    }
                });
#endif
            }); ;

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}