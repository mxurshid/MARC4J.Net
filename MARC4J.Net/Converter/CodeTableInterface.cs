using System;
namespace MARC4J.Net.Converter
{
    public interface ICodeTableInterface : IDisposable
    {
        bool IsCombining(int i, int g0, int g1);
        char GetChar(int c, int mode);
    };
}