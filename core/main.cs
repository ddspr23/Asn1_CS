using decoder_cs.core;

namespace core
{
    class _Main
    {
        static void Main(string[] args)
        {
            Asn1 handle = new Asn1("../../test/iBSS.ipad7b.RELEASE.im4p");
            handle.run();
        }
    }
}