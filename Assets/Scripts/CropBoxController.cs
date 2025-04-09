using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CropBoxController : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    public RectTransform cropBox;

    private Vector2 initialSize;
    private Vector2 initialMousePos;

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cropBox.parent as RectTransform, eventData.position, eventData.pressEventCamera, out initialMousePos
        );
        initialSize = cropBox.sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cropBox.parent as RectTransform, eventData.position, eventData.pressEventCamera, out currentMousePos
        );

        Vector2 delta = currentMousePos - initialMousePos;

        // Resize symmetrically from center
        Vector2 newSize = initialSize + new Vector2(delta.x, delta.y) * 2f;
        newSize = Vector2.Max(newSize, new Vector2(50, 50)); // Minimum size

        cropBox.sizeDelta = newSize;
    }

    // Call this to get crop values
    public Vector4 GetCropAmounts() // left, right, top, bottom in pixels
    {
        Vector3[] cropCorners = new Vector3[4];
        Vector3[] canvasCorners = new Vector3[4];

        cropBox.GetWorldCorners(cropCorners);
        cropBox.GetComponentInParent<Canvas>().GetComponent<RectTransform>().GetWorldCorners(canvasCorners);

        float left = cropCorners[0].x - canvasCorners[0].x;
        float right = canvasCorners[2].x - cropCorners[2].x;
        float bottom = cropCorners[0].y - canvasCorners[0].y;
        float top = canvasCorners[2].y - cropCorners[2].y;

        float percentageLeft = left / canvasCorners[2].x;
        float percentageRight = right / canvasCorners[2].x;
        float percentageTop = top / canvasCorners[2].y;
        float percentageBottom = bottom / canvasCorners[2].y;

        return new Vector4(percentageLeft, percentageRight, percentageTop, percentageBottom);
    }
}
