using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SerializableQueue<T> : Queue<T>, ISerializationCallbackReceiver
{
    [SerializeField] List<T> _items = new List<T>();
    public List<T> items { get { return _items; } }


    public void OnBeforeSerialize()
    {
        items.Clear();
        foreach(T queueItem in this)
        {
            items.Add(queueItem);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();
        foreach(T listItem in items)
        {
            this.Enqueue(listItem);
        }
    }
}