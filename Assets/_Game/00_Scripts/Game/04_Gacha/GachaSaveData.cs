
using System;
using System.Collections.Generic;

[Serializable]
public class BannerPityData
{
    public string gachaId;
    public int pityCount;
}

[Serializable]
public class GachaSaveData
{
    public List<BannerPityData> banners = new();
}