using FenUISharp.Behavior;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text.Model;

namespace FenUISharp.Objects
{
    public static class FContextMenuFactory
    {
        public static FPopupPanel CreateContextMenu()
        {
            FPopupPanel popupPanel = new(() => new(120, 200), hasTail: false, scaleAnimationFromZero: false);
            var stackContentComponent = new StackContentComponent(popupPanel, StackContentComponent.ContentStackType.Vertical, StackContentComponent.ContentStackBehavior.Scroll);

            stackContentComponent.Pad.SetStaticState(5);
            stackContentComponent.Gap.SetStaticState(2.5f);

            new FContextMenuButton(new Text.FText(TextModelFactory.CreateBasic("Test"))).SetParent(popupPanel);
            new FContextMenuButton(new Text.FText(TextModelFactory.CreateBasic("1"))).SetParent(popupPanel);
            new FContextMenuButton(new Text.FText(TextModelFactory.CreateBasic("awpdkapdokwpqokd"))).SetParent(popupPanel);

            stackContentComponent.FullUpdateLayout();

            var currentMousePoint = FContext.GetCurrentWindow().ClientMousePosition;
            popupPanel.Show(() => currentMousePoint);

            return popupPanel;
        }
    }
}