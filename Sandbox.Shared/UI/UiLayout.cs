using Sandbox.Shared.UI.Base;

namespace Sandbox.Shared.UI;

public abstract class UiLayout : UiObject
{
    public abstract IEnumerable<UiObject> GetObjects();
    public abstract void AddObject(UiObject uiObject);

    public abstract void RemoveObject(UiObject uiObject);

    public virtual void AddObjects(params UiObject[] uiObjects)
    {
        foreach (var uiObject in uiObjects)
        {
            AddObject(uiObject);
        }
    }

    public virtual void RemoveObjects(params UiObject[] uiObjects)
    {
        foreach (var uiObject in uiObjects)
        {
            RemoveObject(uiObject);
        }
    }

    protected override void OnDisabled()
    {
        foreach (var uiObject in GetObjects())
        {
            uiObject.Enabled = false;
        }
    }

    protected override void OnEnabled()
    {
        foreach (var uiObject in GetObjects())
        {
            uiObject.Enabled = true;
        }
    }
}