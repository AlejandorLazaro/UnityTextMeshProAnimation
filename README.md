# UnityTextMeshProAnimation

![Example](https://drive.google.com/uc?export=view&id=1HctNd4FdU1C61bGb6EX40FEuZUs_xj5_)

Simple one-file addition to add animation support to Unity's TextMeshPro game objects!

## Install

Simply add the **TextAnimation.cs** file to your Unity script folder!

To apply the animations to a TextMeshPro object, add the component via 'Add Component'&rarr;'Text Animation' and set the animation parameters you like.

## Implementation

You can update text and animation modes from other scripts as per this example:

```c#
// other imports...
using TMPro;
using TMProAnimation;

public class YourClass : MonoBehavior
{
    // Text manager
    TMP_Text textMeshProComponent;
    TextAnimation textAnimation;

    void Start()
    {
        m_textComponent = GameObject.Find("YourTextObject").GetComponent<TMP_Text>();
        m_textAnimation = GameObject.Find("YourTextObject").GetComponent<TextAnimation>();
    }

    void SetAnimation()
    {
        if (/* toggle animation style condition */)
        {
            m_textAnimation.SetAnimationMode(TextAnimation.AnimationMode.Jitter);
        }
        else
        {
            m_textAnimation.StopAnimation();
        }

        if (/* update text */)
        {
            m_textComponent.text += "+";
            m_textAnimation.NotifyTextHasChanged();
        }

    }

}
```

**Note:** You must call `NotifyTextHasChanged()` when changing text to update the TextMeshPro animations.

## Limitations
* This script was built using Coroutines inspired by TextMeshPro examples, which can result in choppy animations due to not running on an Update loop.