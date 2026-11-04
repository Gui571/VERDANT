// Adaptador mínimo para Elevation.cs; operações ECMAScript necessárias ao módulo MIT original.
namespace Fluitown.Runtime;
public static class Js
{
    public static int ToInt32(double d)
    {
        if(!double.IsFinite(d)||d==0)return 0;
        double n=System.Math.Truncate(d)%4294967296.0;
        if(n<0)n+=4294967296.0;
        return unchecked((int)(uint)n);
    }
    public static uint ToUint32(double d)=>unchecked((uint)ToInt32(d));
}
public static class JsMath
{
    public static int imul(int a,int b)=>unchecked(a*b);
    public static double floor(double x)=>System.Math.Floor(x);
}
