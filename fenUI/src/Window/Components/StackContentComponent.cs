using System.Diagnostics;

namespace FenUISharp
{
    public class StackContentComponent : Component
    {
        public enum ContentStackType { Horizontal, Vertical }
        public enum ContentStackBehavior { Overflow, SizeToFit, SizeToFitAll, Scroll }

        public Vector2 StartAlignment { get; set; }
        public float Gap { get; set; } = 10;
        public float Pad { get; set; } = 15;

        public ContentStackType StackType { get; set; }
        public ContentStackBehavior StackBehavior { get; set; }

        public StackContentComponent(UIComponent parent, ContentStackType type, ContentStackBehavior behavior, Vector2? startAlign = null) : base(parent)
        {
            StackType = type;
            StackBehavior = behavior;

            if(startAlign == null){
                if(type == ContentStackType.Horizontal) StartAlignment = new Vector2(0f, 0.5f);
                else if(type == ContentStackType.Vertical) StartAlignment = new Vector2(0.5f, 0f);
            }else{
                StartAlignment = startAlign.Value;
            }
        }

        public void FullUpdateLayout()
        {
            var childList = parent.transform.childs;

            float currentPos = 0;
            float contentSize = 0;
            float contentSizePerpendicular = 0;

            float lastItemSize = 0;
            
            for (int c = 0; c < childList.Count; c++)
            {   
                if (StackType == ContentStackType.Horizontal)
                {
                    lastItemSize = childList[c].localBounds.Width;
                    currentPos += lastItemSize * (c == 0 ? 0.5f : 1);
                    
                    if(c == 0) currentPos += Pad + 1;
                    else currentPos += Gap;

                    contentSizePerpendicular = Math.Max(childList[c].localBounds.Height, contentSizePerpendicular);
    
                    childList[c].alignment = StartAlignment;
                    childList[c].localPosition = new Vector2(currentPos, 0);
                }
                else if (StackType == ContentStackType.Vertical)
                {
                    lastItemSize = childList[c].localBounds.Height;
                    currentPos += lastItemSize * (c == 0 ? 0.5f : 1);

                    if(c == 0) currentPos += Pad;
                    else currentPos += Gap;
                    
                    contentSizePerpendicular = Math.Max(childList[c].localBounds.Width, contentSizePerpendicular);

                    childList[c].alignment = StartAlignment;
                    childList[c].localPosition = new Vector2(0, currentPos);
                }
            }

            contentSize = Math.Abs(currentPos + (lastItemSize / 2) + Pad);
            contentSizePerpendicular += Pad * 2;

            switch(StackBehavior){
                case ContentStackBehavior.SizeToFitAll:
                    if(StackType == ContentStackType.Horizontal) parent.transform.size = new Vector2(contentSize, contentSizePerpendicular);
                    if(StackType == ContentStackType.Vertical) parent.transform.size = new Vector2(contentSizePerpendicular, contentSize);
                    break;
                case ContentStackBehavior.SizeToFit:
                    if(StackType == ContentStackType.Horizontal) parent.transform.size = new Vector2(contentSize, parent.transform.size.y);
                    if(StackType == ContentStackType.Vertical) parent.transform.size = new Vector2(parent.transform.size.x, contentSize);
                    break;
            }

            parent.Invalidate();
            Console.WriteLine("update");
            Console.WriteLine(parent.GetType().FullName);
        }
    }
}