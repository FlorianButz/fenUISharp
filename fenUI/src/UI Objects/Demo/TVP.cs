
using FenUISharp.Behavior;
using FenUISharp.Components;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Materials;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Layout;
using FenUISharp.Objects.Text.Model;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;

namespace FenUISharp.Objects
{
    public class DemoViewPane : View
    {
        public override List<UIObject> Create()
        {
            FPanel panel = new();
            panel.RenderMaterial.SetStaticState(new EmptyDefaultMaterial() { BaseColor = () => SKColors.Transparent });
            panel.Layout.StretchHorizontal.SetStaticState(true);
            panel.Layout.StretchVertical.SetStaticState(true);
            panel.Layout.MarginVertical.SetStaticState(25);

            StackContentComponent layout = new(panel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);

            {
                FText title = new(TextModelFactory.CreateBasic("Images", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                FImage img1 = new(() => Resources.GetImage("test-img"), size: () => new(75, 75));
                img1.ScaleMode.SetStaticState(FImage.ImageScaleMode.Stretch);
                img1.SetParent(subpanel);

                FImage img3 = new(() => Resources.GetImage("test-img"), size: () => new(133, 75));
                img1.ScaleMode.SetStaticState(FImage.ImageScaleMode.Contain);
                img3.SetParent(subpanel);

                FImage img2 = new(() => Resources.GetImage("test-img"), size: () => new(75, 75));
                img1.ScaleMode.SetStaticState(FImage.ImageScaleMode.Fit);
                img2.SetParent(subpanel);

                sublayout.FullUpdateLayout();
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Text", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.Transform.Size.SetStaticState(new(500, 0));
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFit);
                sublayout.Gap.SetStaticState(25);
                sublayout.Pad.SetStaticState(30);

                FText text1 = new(TextModelFactory.CreateBasic("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est"), size: () => new(0, 100));
                text1.Layout.StretchHorizontal.SetStaticState(true);
                text1.Layout.MarginHorizontal.SetStaticState(65);
                text1.Transform.Size.SetStaticState(new(0, text1.LayoutModel.GetBoundingRect(text1.Model, SKRect.Create(subpanel.Transform.Size.CachedValue.x, 250)).Height));
                text1.SetParent(subpanel);
                
                FText text2 = new(TextModelFactory.CreateTest("Multiple text styles in one!"), size: () => new(0, 100));
                text2.Layout.StretchHorizontal.SetStaticState(true);
                text2.Layout.MarginHorizontal.SetStaticState(65);
                text2.Transform.Size.SetStaticState(new(0, text2.LayoutModel.GetBoundingRect(text2.Model, SKRect.Create(subpanel.Transform.Size.CachedValue.x, 250)).Height));
                text2.SetParent(subpanel);
                
                FText text3 = new(TextModelFactory.CreateBasic("Dynamic text processors"), size: () => new(0, 100));
                text3.Layout.StretchHorizontal.SetStaticState(true);
                text3.LayoutModel = new WiggleCharsLayoutProcessor(text3, new WrapLayout(text3));
                text3.Layout.MarginHorizontal.SetStaticState(65);
                text3.Transform.Size.SetStaticState(new(0, text3.LayoutModel.GetBoundingRect(text3.Model, SKRect.Create(subpanel.Transform.Size.CachedValue.x, 250)).Height));
                text3.SetParent(subpanel);
                
                FText text4 = new(TextModelFactory.CreateBasic("Text change animation (Text 1)"), size: () => new(0, 100));
                text4.Layout.StretchHorizontal.SetStaticState(true);
                text4.LayoutModel = new BlurLayoutProcessor(text4, new WrapLayout(text4));
                text4.Layout.MarginHorizontal.SetStaticState(65);
                text4.Transform.Size.SetStaticState(new(0, text4.LayoutModel.GetBoundingRect(text4.Model, SKRect.Create(subpanel.Transform.Size.CachedValue.x, 250)).Height));
                text4.SetParent(subpanel);

                float val = 0;
                int lastText = 0;
                FContext.GetCurrentWindow().OnPreUpdate += () =>
                {
                    val = ((float)Math.Sin(FContext.Time) + 1) / 2 + 1.5f;

                    int text = (int)val;
                    if (lastText != text)
                        text4.Model = TextModelFactory.CreateBasic($"{Math.Round(Random.Shared.NextSingle() * 10) / 10} <- Random number\n Text change animation (Text {text})");
                    lastText = text;
                };

                sublayout.FullUpdateLayout();
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Sliders / Scrollers", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.Transform.Size.SetStaticState(new(500, 0));
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFit);
                sublayout.Pad.SetStaticState(25);
                sublayout.Gap.SetStaticState(30);

                FSlider slider1 = new(width: 150);
                slider1.SetParent(subpanel);

                FSlider slider2 = new(width: 150);
                slider2.SnappingInterval = 0.25f;
                slider2.KnobSize = new(slider2.KnobSize.x / 2.5f, slider2.KnobSize.y);
                slider2.SetParent(subpanel);

                FNumericScroller scroller1 = new(new FText(TextModelFactory.CreateBasic("")), () => "C");
                scroller1.MaxValue.SetStaticState(100);
                scroller1.Value = 10;
                scroller1.SetParent(subpanel);

                sublayout.FullUpdateLayout();
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Toggles", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.Transform.Size.SetStaticState(new(500, 0));
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFit);
                sublayout.Pad.SetStaticState(25);
                sublayout.Gap.SetStaticState(30);

                FRoundToggle t1 = new();
                t1.SetParent(subpanel);

                FToggle t2 = new();
                t2.SetParent(subpanel);

                sublayout.FullUpdateLayout();
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Button Groups", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                {
                    FPanel subpanel = new();
                    subpanel.SetParent(panel);

                    StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                    FRoundToggle toggle1 = new();
                    toggle1.SetParent(subpanel);

                    FRoundToggle toggle2 = new();
                    toggle2.SetParent(subpanel);

                    FRoundToggle toggle3 = new();
                    toggle3.SetParent(subpanel);

                    FButtonGroup btnGroup = new();
                    btnGroup.Add(toggle1);
                    btnGroup.Add(toggle2);
                    btnGroup.Add(toggle3);

                    btnGroup.AllowMultiSelect = false;
                    btnGroup.AlwaysMustSelectOne = true;

                    sublayout.FullUpdateLayout();
                }

                {
                    FPanel subpanel = new();
                    subpanel.SetParent(panel);

                    StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                    FRoundToggle toggle1 = new();
                    toggle1.SetParent(subpanel);

                    FRoundToggle toggle2 = new();
                    toggle2.SetParent(subpanel);

                    FRoundToggle toggle3 = new();
                    toggle3.SetParent(subpanel);

                    FButtonGroup btnGroup = new();
                    btnGroup.Add(toggle1);
                    btnGroup.Add(toggle2);
                    btnGroup.Add(toggle3);

                    btnGroup.AllowMultiSelect = true;
                    btnGroup.AlwaysMustSelectOne = true;

                    sublayout.FullUpdateLayout();
                }

                {
                    FPanel subpanel = new();
                    subpanel.SetParent(panel);

                    StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Horizontal, StackContentComponent.ContentStackBehavior.SizeToFitAll);

                    FRoundToggle toggle1 = new();
                    toggle1.SetParent(subpanel);

                    FRoundToggle toggle2 = new();
                    toggle2.SetParent(subpanel);

                    FRoundToggle toggle3 = new();
                    toggle3.SetParent(subpanel);

                    FButtonGroup btnGroup = new();
                    btnGroup.Add(toggle1);
                    btnGroup.Add(toggle2);
                    btnGroup.Add(toggle3);

                    btnGroup.AllowMultiSelect = false;
                    btnGroup.AlwaysMustSelectOne = false;

                    sublayout.FullUpdateLayout();
                }
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Segmented Controls", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.Transform.Size.SetStaticState(new(500, 0));
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFit);
                sublayout.Pad.SetStaticState(10);
                sublayout.Gap.SetStaticState(5);

                FSegmentedControl segmentedControl1 = new(new(new()
                {
                    [TextModelFactory.CreateBasic("B", bold: true)] = (x) => { },
                    [TextModelFactory.CreateBasic("U", underlined: true)] = (x) => { },
                    [TextModelFactory.CreateBasic("I", italic: true)] = (x) => { }
                }), 2);
                segmentedControl1.SetParent(subpanel);

                FSegmentedControl segmentedControl2 = new FSegmentedControl(new(new()
                {
                    [TextModelFactory.CreateBasic("Text 1")] = (x) => { },
                    [TextModelFactory.CreateBasic("Text 2")] = (x) => { },
                    [TextModelFactory.CreateBasic("Text 2")] = (x) => { }
                }), 2);
                segmentedControl2.SetParent(subpanel);

                sublayout.FullUpdateLayout();
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Other Controls", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.Transform.Size.SetStaticState(new(500, 0));
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFit);
                sublayout.Pad.SetStaticState(25);
                sublayout.Gap.SetStaticState(30);

                FColorPatch colorPatch = new(SKColors.Red);
                colorPatch.SetParent(subpanel);

                FTextInputField textInputField = new(new FText(TextModelFactory.CreateBasic("")));
                textInputField.SetParent(subpanel);
                FTextInputField textInputFieldPassword = new(new FText(TextModelFactory.CreateBasic(""))) { PlaceholderText = "Type in password...", TextInputMode = FTextInputField.TextInputFieldMode.Password };
                textInputFieldPassword.SetParent(subpanel);

                sublayout.FullUpdateLayout();
            }

            {
                FText title = new(TextModelFactory.CreateBasic("Theme", 20, bold: true), size: () => new Vector2(200, 75));
                title.SetParent(panel);

                FPanel subpanel = new();
                subpanel.Transform.Size.SetStaticState(new(500, 0));
                subpanel.SetParent(panel);

                StackContentComponent sublayout = new(subpanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.SizeToFit);
                sublayout.Pad.SetStaticState(10);
                sublayout.Gap.SetStaticState(5);

                FSegmentedControl segmentedControl1 = new(new(new()
                {
                    [TextModelFactory.CreateBasic("Light")] = (x) => { FContext.GetCurrentWindow().WindowThemeManager.SetTheme(Resources.GetTheme("default-light")); FContext.GetCurrentWindow().SystemDarkMode = false; },
                    [TextModelFactory.CreateBasic("Dark")] = (x) => { FContext.GetCurrentWindow().WindowThemeManager.SetTheme(Resources.GetTheme("default-dark")); FContext.GetCurrentWindow().SystemDarkMode = true; }
                }), 1);
                segmentedControl1.SetParent(subpanel);

                sublayout.FullUpdateLayout();
            }

            // TODO: Fix layout update order
            FContext.GetCurrentDispatcher().InvokeLater(() => layout.FullUpdateLayout(), 2L);
            return new List<UIObject>() { panel };
        }
    }
}