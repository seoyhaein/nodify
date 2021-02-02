using System.Windows;
using System.Windows.Controls;

namespace Nodify
{
    public class NodifyCanvas : Panel
    {
        static NodifyCanvas()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NodifyCanvas), new FrameworkPropertyMetadata(typeof(NodifyCanvas)));
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                // 우리는 Children 대신 InternalChildren을 사용하여 자식 컬렉션을 얻습니다. 
                // 그 이유는 InternalChildren에는 Children 컬렉션에있는 모든 항목과 데이터 바인딩을 통해 추가 된 자식이 포함되어 있기 때문입니다.
                var internalChild = (ItemContainer)InternalChildren[i];  // NodifyCanvas 내의 자식들이 ItemContainer 라는 소리야!!
                internalChild.Arrange(new Rect(internalChild.Location, internalChild.DesiredSize));  // NodifyCanvas 내에서 interalChild 의 크기와 위치를 지정한다.
            }

            return arrangeSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            // NodifyCanvas 내의 자식들의 사이즈를 무한대로(Auto) 지정해준다.
            Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                InternalChildren[i].Measure(availableSize);
            }

            return default;
        }
    }
}
