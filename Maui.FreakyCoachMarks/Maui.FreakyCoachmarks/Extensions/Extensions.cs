using Maui.FreakyCoachMarks.Effects;

#if ANDROID
using BlurPlatformEffect = Maui.FreakyCoachMarks.Platforms.Android.BlurPlatformEffect;
#elif IOS
using BlurPlatformEffect = Maui.FreakyCoachMarks.Platforms.iOS.BlurPlatformEffect;
#elif MACCATALYST
using Maui.FreakyCoachMarks.Platforms.MacCatalyst;
#elif WINDOWS
using Maui.FreakyCoachMarks.Platforms.Windows;
#else

using Maui.FreakyCoachMarks.Platforms.Dotnet;

#endif

namespace Maui.FreakyCoachMarks.Extensions;

public static class Extensions
{
    public static MauiAppBuilder UseFreakyCoachMarks(this MauiAppBuilder builder)
    {
        builder.ConfigureEffects(AddEffects);
        return builder;
    }

    private static void AddEffects(IEffectsBuilder effects)
    {
        effects.Add<BlurEffect, BlurPlatformEffect>();
    }
}