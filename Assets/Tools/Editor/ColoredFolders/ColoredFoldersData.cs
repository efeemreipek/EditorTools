using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ColoredFolderEntry
{
    public string Path;
    public string GUID;
    public Color Color;
}

public class ColoredFoldersData : ScriptableObject
{
    public List<ColoredFolderEntry> Data = new List<ColoredFolderEntry>();
}