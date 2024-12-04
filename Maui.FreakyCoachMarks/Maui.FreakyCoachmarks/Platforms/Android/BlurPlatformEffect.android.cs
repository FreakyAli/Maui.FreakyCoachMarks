using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Renderscripts;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Annotations;
using Java.Lang;
using Maui.FreakyCoachMarks.Effects;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using Color = Android.Graphics.Color;
using Element = Android.Renderscripts.Element;
using Paint = Android.Graphics.Paint;
using View = Android.Views.View;

namespace Maui.FreakyCoachMarks.Platforms.Android;

internal class BlurPlatformEffect : PlatformEffect
{
    public Context Context => Control?.Context;

    private BlurView _blurView;
    private GradientDrawable _mainDrawable;

    public BlurEffect VirtualEffect { get; private set; }

    protected override void OnAttached()
    {
        if (Element.Effects.FirstOrDefault(x => x.ResolveId == this.ResolveId) is BlurEffect blurEffect)
        {
            VirtualEffect = blurEffect;
            blurEffect.UpdateEffectCommand = new Command(() =>
            {
                _blurView.SetBackgroundColor(GetColor());
                //AlignBlurView();
            });
        }

        if (Element is Microsoft.Maui.Controls.View view)
        {
            view.SizeChanged += BlurPlatformEffect_SizeChanged;
            view.ParentChanged += View_ParentChanged;
        }

        UpdateEffect();
    }

    protected override void OnDetached()
    {
        if (Element is Microsoft.Maui.Controls.View view)
        {
            view.SizeChanged -= BlurPlatformEffect_SizeChanged;
            view.ParentChanged -= View_ParentChanged;
        }

        //TODO: Release drawable
    }

    private void BlurPlatformEffect_SizeChanged(object sender, EventArgs e)
    {
        AlignBlurView();
    }

    private void View_ParentChanged(object sender, EventArgs e)
    {
        UpdateEffect();
    }

    protected void UpdateEffect()
    {
        if (Control is ViewGroup viewGroup)
        {
            if (_mainDrawable == null)
            {
                _mainDrawable = new GradientDrawable();
                _mainDrawable.SetColor(Colors.Transparent.ToAndroid());
                Control.Background = _mainDrawable;
            }

            if (_blurView == null)
            {
                _blurView = new BlurView(Context);

                //var child = viewGroup.GetChildAt(0) ?? new Android.Views.View(Context);
                //child.RemoveFromParent();
                //_blurView.AddView(child);

                while (viewGroup.GetChildAt(0) != null)
                {
                    var child = viewGroup.GetChildAt(0);
                    child.RemoveFromParent();
                    _blurView.AddView(child);
                }

                viewGroup.AddView(_blurView, 0, new FrameLayout.LayoutParams(
                        ViewGroup.LayoutParams.FillParent,
                        ViewGroup.LayoutParams.FillParent,
                        GravityFlags.NoGravity));

                _blurView.SetOverlayColor(Color.Transparent);
                AlignBlurView();
            }

            var decorView = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.Window.DecorView;
            var root = decorView.FindViewById(global::Android.Resource.Id.Content) as global::Android.Views.ViewGroup;
            var windowBackground = decorView.Background;

            _blurView.SetBackgroundColor(GetColor());

            _blurView
               .SetupWith(root as global::Android.Views.ViewGroup, new RenderScriptBlur(Context))
               .SetFrameClearDrawable(windowBackground) // Optional
               .SetBlurRadius(24f);
        }
        else
        {
            // Not supported for Standalone Views.
        }
    }

    protected Color GetColor()
    {
        if (VirtualEffect?.AccentColor != null && VirtualEffect.AccentColor.IsNotDefault())
        {
            return VirtualEffect.AccentColor.WithAlpha(VirtualEffect.AccentOpacity).ToAndroid();
        }

        return VirtualEffect?.Mode == BlurMode.Dark
            ? Colors.Black.WithAlpha(VirtualEffect.AccentOpacity).ToAndroid()
            : Colors.White.WithAlpha(VirtualEffect.AccentOpacity).ToAndroid();
    }

    private void AlignBlurView()
    {
        var PlatformView = Control;

        //if (PlatformView.MeasuredWidth == 0 || PlatformView.MeasuredHeight == 0 || _blurView == null)
        //{
        //    return;
        //}
        if (_blurView == null)
        {
            return;
        }

        int width = PlatformView.MeasuredWidth;
        int height = PlatformView.MeasuredHeight;
        _blurView.Measure(width, height);
        _blurView.Layout(0, 0, width, height);
    }
}

internal class BlurViewCanvas : Canvas
{
    public BlurViewCanvas(Bitmap bitmap) : base(bitmap)
    {
    }
}

internal class PreDrawBlurController : IBlurController
{
    public const int TRANSPARENT = 0;

    private float blurRadius = BlurViewDefaults.BLUR_RADIUS;

    private readonly IBlurAlgorithm blurAlgorithm;
    private BlurViewCanvas internalCanvas;
    private Bitmap internalBitmap;

    View blurView;
    private Color overlayColor;
    private ViewGroup rootView;
    private int[] rootLocation = new int[2];
    private int[] blurViewLocation = new int[2];

    private bool blurEnabled = true;
    private bool initialized;

    private Drawable frameClearDrawable;

    private readonly OnPreDrawListener drawListener;
    public PreDrawBlurController(View blurView, ViewGroup rootView, Color overlayColor, IBlurAlgorithm algorithm)
    {
        drawListener = new OnPreDrawListener(() =>
        {
            UpdateBlur();
        });

        this.rootView = rootView;
        this.blurView = blurView;
        this.overlayColor = overlayColor;
        this.blurAlgorithm = algorithm;

        if (algorithm is RenderEffectBlur renderEffectBlur)
        {
            renderEffectBlur.SetContext(blurView.Context);
        }

        int measuredWidth = blurView.MeasuredWidth;
        int measuredHeight = blurView.MeasuredHeight;

        Init(measuredWidth, measuredHeight);
    }

    void Init(int measuredWidth, int measuredHeight)
    {
        SetBlurAutoUpdate(true);
        SizeScaler sizeScaler = new SizeScaler(blurAlgorithm.ScaleFactor);
        if (sizeScaler.IsZeroSized(measuredWidth, measuredHeight))
        {
            // Will be initialized later when the View reports a size change
            blurView.SetWillNotDraw(true);
            return;
        }

        blurView.SetWillNotDraw(false);
        SizeScaler.Size bitmapSize = sizeScaler.scale(measuredWidth, measuredHeight);
        internalBitmap = Bitmap.CreateBitmap(bitmapSize.width, bitmapSize.height, blurAlgorithm.SupportedBitmapConfig);
        internalCanvas = new BlurViewCanvas(internalBitmap);
        initialized = true;
        // Usually it's not needed, because `onPreDraw` updates the blur anyway.
        // But it handles cases when the PreDraw listener is attached to a different Window, for example
        // when the BlurView is in a Dialog window, but the root is in the Activity.
        // Previously it was done in `draw`, but it was causing potential side effects and Jetpack Compose crashes
        UpdateBlur();
    }

    void UpdateBlur()
    {
        if (!blurEnabled || !initialized)
        {
            return;
        }

        if (frameClearDrawable == null)
        {
            internalBitmap.EraseColor(global::Android.Graphics.Color.Transparent);
        }
        else
        {
            frameClearDrawable.Draw(internalCanvas);
        }

        internalCanvas.Save();
        SetupInternalCanvasMatrix();
        rootView.Draw(internalCanvas);
        internalCanvas.Restore();

        BlurAndSave();
    }
    /**
    * Set up matrix to draw starting from blurView's position
    */
    private void SetupInternalCanvasMatrix()
    {
        rootView.GetLocationOnScreen(rootLocation);
        blurView.GetLocationOnScreen(blurViewLocation);

        int left = blurViewLocation[0] - rootLocation[0];
        int top = blurViewLocation[1] - rootLocation[1];

        // https://github.com/Dimezis/BlurView/issues/128
        float scaleFactorH = (float)blurView.Height / internalBitmap.Height;
        float scaleFactorW = (float)blurView.Width / internalBitmap.Width;

        float scaledLeftPosition = -left / scaleFactorW;
        float scaledTopPosition = -top / scaleFactorH;

        internalCanvas.Translate(scaledLeftPosition, scaledTopPosition);
        internalCanvas.Scale(1 / scaleFactorW, 1 / scaleFactorH);
    }

    public bool Draw(Canvas canvas)
    {
        if (!blurEnabled || !initialized)
        {
            return true;
        }
        // Not blurring itself or other BlurViews to not cause recursive draw calls
        // Related: https://github.com/Dimezis/BlurView/issues/110
        if (canvas is BlurViewCanvas)
        {
            return false;
        }

        // https://github.com/Dimezis/BlurView/issues/128
        float scaleFactorH = (float)blurView.Height / internalBitmap.Height;
        float scaleFactorW = (float)blurView.Width / internalBitmap.Width;

        canvas.Save();
        canvas.Scale(scaleFactorW, scaleFactorH);
        blurAlgorithm.Render(canvas, internalBitmap);
        canvas.Restore();
        if (overlayColor != TRANSPARENT)
        {
            canvas.DrawColor(overlayColor);
        }
        return true;
    }

    private void BlurAndSave()
    {
        internalBitmap = blurAlgorithm.Blur(internalBitmap, blurRadius);
        if (!blurAlgorithm.CanModifyBitmap)
        {
            internalCanvas.SetBitmap(internalBitmap);
        }
    }

    public void UpdateBlurViewSize()
    {
        int measuredWidth = blurView.MeasuredWidth;
        int measuredHeight = blurView.MeasuredHeight;

        Init(measuredWidth, measuredHeight);
    }

    public void Destroy()
    {
        SetBlurAutoUpdate(false);
        blurAlgorithm.Destroy();
        initialized = false;
    }

    public IBlurViewFacade SetBlurRadius(float radius)
    {
        this.blurRadius = radius;
        return this;
    }

    public IBlurViewFacade SetFrameClearDrawable(Drawable frameClearDrawable)
    {
        this.frameClearDrawable = frameClearDrawable;
        return this;
    }

    public IBlurViewFacade SetBlurEnabled(bool enabled)
    {
        this.blurEnabled = enabled;
        SetBlurAutoUpdate(enabled);
        blurView.Invalidate();
        return this;
    }

    public IBlurViewFacade SetBlurAutoUpdate(bool enabled)
    {
        rootView.ViewTreeObserver.RemoveOnPreDrawListener(drawListener);
        if (enabled)
        {
            rootView.ViewTreeObserver.AddOnPreDrawListener(drawListener);
        }
        return this;
    }

    public IBlurViewFacade SetOverlayColor(Color overlayColor)
    {
        if (this.overlayColor != overlayColor)
        {
            this.overlayColor = overlayColor;
            blurView.Invalidate();
        }
        return this;
    }
}

internal interface IBlurController : IBlurViewFacade
{
    /**
  * Draws blurred content on given canvas
  *
  * @return true if BlurView should proceed with drawing itself and its children
  */
    bool Draw(Canvas canvas);

    /**
     * Must be used to notify Controller when BlurView's size has changed
     */
    void UpdateBlurViewSize();

    /**
     * Frees allocated resources
     */
    void Destroy();
}

internal static class BlurViewDefaults
{
    public const float SCALE_FACTOR = 6f;
    public const float BLUR_RADIUS = 16f;
}

internal class NoOpController : IBlurController
{
    public bool Draw(Canvas canvas)
    {
        return true;
    }

    public void Destroy()
    {
    }

    public IBlurViewFacade SetBlurAutoUpdate(bool enabled)
    {
        return this;
    }

    public IBlurViewFacade SetBlurEnabled(bool enabled)
    {
        return this;
    }

    public IBlurViewFacade SetBlurRadius(float radius)
    {
        return this;
    }

    public IBlurViewFacade SetFrameClearDrawable(Drawable frameClearDrawable)
    {
        return this;
    }

    public IBlurViewFacade SetOverlayColor(Color overlayColor)
    {
        return this;
    }

    public void UpdateBlurViewSize()
    {
    }
}

internal class OnPreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
{
    Action actionOnPreDraw;

    public OnPreDrawListener(Action actionOnPreDraw)
    {
        this.actionOnPreDraw = actionOnPreDraw;
    }

    public bool OnPreDraw()
    {
        actionOnPreDraw?.Invoke();
        return true;
    }
}

internal interface IBlurViewFacade
{
    IBlurViewFacade SetBlurEnabled(bool enabled);

    IBlurViewFacade SetBlurAutoUpdate(bool enabled);

    IBlurViewFacade SetFrameClearDrawable(Drawable frameClearDrawable);

    IBlurViewFacade SetBlurRadius(float radius);

    IBlurViewFacade SetOverlayColor(Color overlayColor);
}

internal interface IBlurAlgorithm
{
    Bitmap Blur(Bitmap bitmapExportContext, float blurRadius);

    void Destroy();

    bool CanModifyBitmap { get; }

    Bitmap.Config SupportedBitmapConfig { get; }

    float ScaleFactor { get; }

    void Render(Canvas canvas, Bitmap bitmap);
}

internal class BlurView : FrameLayout
{
    private static string TAG = typeof(BlurView).Name;

    IBlurController blurController = new NoOpController();

    private Color overlayColor;

    public BlurView(Context context) : base(context)
    {
    }

    public BlurView(Context context, IAttributeSet attrs) : base(context, attrs)
    {
    }

    public BlurView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
    {
    }

    public override void Draw(Canvas canvas)
    {
        var shouldDraw = blurController.Draw(canvas);
        if (shouldDraw)
        {
            base.Draw(canvas);
        }
    }

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);
        blurController.UpdateBlurViewSize();
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        if (!IsHardwareAccelerated)
        {
            Log.Error(TAG, "BlurView can't be used in not hardware-accelerated window!");
        }
        else
        {
            blurController.SetBlurAutoUpdate(true);
        }
    }

    /**
   * @param rootView  root to start blur from.
   *                  Can be Activity's root content layout (android.R.id.content)
   *                  or (preferably) some of your layouts. The lower amount of Views are in the root, the better for performance.
   * @param algorithm sets the blur algorithm
   * @return {@link BlurView} to setup needed params.
   */
    public IBlurViewFacade SetupWith(ViewGroup rootView, IBlurAlgorithm algorithm)
    {
        this.blurController.Destroy();
        var blurController = new PreDrawBlurController(this, rootView, overlayColor, algorithm);
        this.blurController = blurController;

        return blurController;
    }

    /**
    * @param rootView root to start blur from.
    *                 Can be Activity's root content layout (android.R.id.content)
    *                 or (preferably) some of your layouts. The lower amount of Views are in the root, the better for performance.
    *                 <p>
    *                 BlurAlgorithm is automatically picked based on the API version.
    *                 It uses RenderEffectBlur on API 31+, and RenderScriptBlur on older versions.
    * @return {@link BlurView} to setup needed params.
    */

    [RequiresApi(Value = (int)BuildVersionCodes.JellyBeanMr1)]
    public IBlurViewFacade SetupWith(ViewGroup rootView)
    {
        return SetupWith(rootView, GetBlurAlgorithm());
    }

    /**
  * @see BlurViewFacade#setBlurRadius(float)
  */
    public IBlurViewFacade SetBlurRadius(float radius)
    {
        return blurController.SetBlurRadius(radius);
    }

    /**
    * @see BlurViewFacade#setOverlayColor(int)
    */
    public IBlurViewFacade SetOverlayColor(Color overlayColor)
    {
        this.overlayColor = overlayColor;
        return blurController.SetOverlayColor(overlayColor);
    }

    /**
     * @see BlurViewFacade#setBlurAutoUpdate(boolean)
     */
    public IBlurViewFacade setBlurAutoUpdate(bool enabled)
    {
        return blurController.SetBlurAutoUpdate(enabled);
    }

    /**
     * @see BlurViewFacade#setBlurEnabled(boolean)
     */
    public IBlurViewFacade setBlurEnabled(bool enabled)
    {
        return blurController.SetBlurEnabled(enabled);
    }

    [RequiresApi(Value = (int)BuildVersionCodes.JellyBeanMr1)]
    private IBlurAlgorithm GetBlurAlgorithm()
    {
        IBlurAlgorithm algorithm;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            algorithm = new RenderEffectBlur();
        }
        else
        {
            algorithm = new RenderScriptBlur(Context);
        }
        return algorithm;
    }
}

internal class RenderEffectBlur : IBlurAlgorithm
{
    private readonly RenderNode node = new RenderNode("BlurViewNode");

    private int height, width;
    private float lastBlurRadius = 1f;

    public IBlurAlgorithm fallbackAlgorithm;
    private Context context;

    public Bitmap Blur(Bitmap bitmap, float blurRadius)
    {
        lastBlurRadius = blurRadius;

        if (bitmap.Height != height || bitmap.Width != width)
        {
            height = bitmap.Height;
            width = bitmap.Width;
            node.SetPosition(0, 0, width, height);
        }
        Canvas canvas = node.BeginRecording();
        canvas.DrawBitmap(bitmap, 0, 0, null);
        node.EndRecording();
        node.SetRenderEffect(RenderEffect.CreateBlurEffect(blurRadius, blurRadius, Shader.TileMode.Mirror));
        // returning not blurred bitmap, because the rendering relies on the RenderNode
        return bitmap;
    }

    public void Destroy()
    {
        node.DiscardDisplayList();
        if (fallbackAlgorithm != null)
        {
            fallbackAlgorithm.Destroy();
        }
    }

    public bool CanModifyBitmap => true;

    public Bitmap.Config SupportedBitmapConfig => Bitmap.Config.Argb8888;

    public float ScaleFactor => BlurViewDefaults.SCALE_FACTOR;

    public void Render(Canvas canvas, Bitmap bitmap)
    {
        if (canvas.IsHardwareAccelerated)
        {
            canvas.DrawRenderNode(node);
        }
        else
        {
            if (fallbackAlgorithm == null)
            {
                fallbackAlgorithm = new RenderScriptBlur(context);
            }
            fallbackAlgorithm.Blur(bitmap, lastBlurRadius);
            fallbackAlgorithm.Render(canvas, bitmap);
        }
    }

    public void SetContext(Context context)
    {
        this.context = context;
    }
}

internal class RenderScriptBlur : IBlurAlgorithm
{
    private readonly Paint paint = new Paint(PaintFlags.FilterBitmap);
    private readonly RenderScript renderScript;
    private readonly ScriptIntrinsicBlur blurScript;
    private Allocation outAllocation;

    private int lastBitmapWidth = -1;
    private int lastBitmapHeight = -1;

    public RenderScriptBlur(Context context)
    {
        renderScript = RenderScript.Create(context);
        blurScript = ScriptIntrinsicBlur.Create(renderScript, Element.U8_4(renderScript));
    }

    private bool canReuseAllocation(Bitmap bitmap)
    {
        return bitmap.Height == lastBitmapHeight && bitmap.Width == lastBitmapWidth;
    }

    /**
     * @param bitmap     bitmap to blur
     * @param blurRadius blur radius (1..25)
     * @return blurred bitmap
     */

    [RequiresApi(Value = (int)BuildVersionCodes.JellyBeanMr1)]
    public Bitmap Blur(Bitmap bitmap, float blurRadius)
    {
        //Allocation will use the same backing array of pixels as bitmap if created with USAGE_SHARED flag
        Allocation inAllocation = Allocation.CreateFromBitmap(renderScript, bitmap);

        if (!canReuseAllocation(bitmap))
        {
            if (outAllocation != null)
            {
                outAllocation.Destroy();
            }
            outAllocation = Allocation.CreateTyped(renderScript, inAllocation.Type);
            lastBitmapWidth = bitmap.Width;
            lastBitmapHeight = bitmap.Height;
        }

        blurScript.SetRadius(blurRadius);
        blurScript.SetInput(inAllocation);
        //do not use inAllocation in forEach. it will cause visual artifacts on blurred Bitmap
        blurScript.ForEach(outAllocation);
        outAllocation.CopyTo(bitmap);

        inAllocation.Destroy();
        return bitmap;
    }

    public void Destroy()
    {
        blurScript.Destroy();
        renderScript.Destroy();
        if (outAllocation != null)
        {
            outAllocation.Destroy();
        }
    }

    public bool CanModifyBitmap => true;

    public Bitmap.Config SupportedBitmapConfig => Bitmap.Config.Argb8888;

    public float ScaleFactor => BlurViewDefaults.SCALE_FACTOR;

    public void Render(Canvas canvas, Bitmap bitmap)
    {
        canvas.DrawBitmap(bitmap, 0f, 0f, paint);
    }
}

internal class SizeScaler
{
    // Bitmap size should be divisible by ROUNDING_VALUE to meet stride requirement.
    // This will help avoiding an extra bitmap allocation when passing the bitmap to RenderScript for blur.
    // Usually it's 16, but on Samsung devices it's 64 for some reason.
    private static readonly int ROUNDING_VALUE = 64;
    private readonly float scaleFactor;

    public SizeScaler(float scaleFactor)
    {
        this.scaleFactor = scaleFactor;
    }

    public Size scale(int width, int height)
    {
        int nonRoundedScaledWidth = downscaleSize(width);
        int scaledWidth = roundSize(nonRoundedScaledWidth);
        //Only width has to be aligned to ROUNDING_VALUE
        float roundingScaleFactor = (float)width / scaledWidth;
        //Ceiling because rounding or flooring might leave empty space on the View's bottom
        int scaledHeight = (int)Java.Lang.Math.Ceil(height / roundingScaleFactor);

        return new Size(scaledWidth, scaledHeight, roundingScaleFactor);
    }

    public bool IsZeroSized(int measuredWidth, int measuredHeight)
    {
        return downscaleSize(measuredHeight) == 0 || downscaleSize(measuredWidth) == 0;
    }

    /**
     * Rounds a value to the nearest divisible by {@link #ROUNDING_VALUE} to meet stride requirement
     */
    private int roundSize(int value)
    {
        if (value % ROUNDING_VALUE == 0)
        {
            return value;
        }
        return value - (value % ROUNDING_VALUE) + ROUNDING_VALUE;
    }

    private int downscaleSize(float value)
    {
        return (int)Java.Lang.Math.Ceil(value / scaleFactor);
    }

    public class Size
    {
        public int width;
        public int height;
        // TODO this is probably not needed anymore
        float scaleFactor;

        public Size(int width, int height, float scaleFactor)
        {
            this.width = width;
            this.height = height;
            this.scaleFactor = scaleFactor;
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            Size size = (Size)o;

            if (width != size.width) return false;
            if (height != size.height) return false;
            return Float.Compare(size.scaleFactor, scaleFactor) == 0;
        }

        public override int GetHashCode()
        {
            int result = width;
            result = 31 * result + height;
            result = 31 * result + (scaleFactor != +0.0f ? Float.FloatToIntBits(scaleFactor) : 0);
            return result;
        }

        public override string ToString()
        {
            return "Size{" +
                    "width=" + width +
                    ", height=" + height +
                    ", scaleFactor=" + scaleFactor +
                    '}';
        }
    }
}