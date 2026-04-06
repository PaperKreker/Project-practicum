using System.Collections.Generic;

public static class SigilDatabase
{
    // All sigils available for purchase. Add new ones here.
    public static List<Sigil> All() => new List<Sigil>
    {
        new SigilTheMoon(),
        new SigilTheSun(),
        new SigilIsolation(),
        new SigilDyadic(),
        new SigilTriadic(),
        new SigilAlignment(),
        new SigilFlow(),
        new SigilMalice(),
        new SigilRage(),
        new SigilConformity(),
        new SigilFortification(),
        new SigilMediation(),
        new SigilRefusal(),
        new SigilPower(),
        new SigilConversion(),
    };
}
