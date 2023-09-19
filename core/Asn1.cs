using System;
using System.Collections.Generic;
using System.IO;

namespace decoder_cs.core
{
    internal class Asn1
    {
        private string _filename;
        private byte[] _buffer;
        private long _length;
        private long _cp;

        private FileStream _fs;
        private TAG _t;

        readonly private List<TAG> _sequence;

        private struct TAG
        {
            public TAG(uint a, uint b, IntPtr c, bool d)
            {
                _type = a;
                _length = b;
                unsafe
                {
                    _value = (void*)c;
                }
                _constructed = d;
            }

            public void _flush()
            {
                this._type = 0;
                this._length = 0;
                unsafe { this._value = (void*)IntPtr.Zero; }
                this._constructed = false;
            }

            public uint _type;
            public uint _length;

            public unsafe void* _value; // don't try this at home kids.
            public bool _constructed;
        }

        public Asn1(string filename)
        {
            _filename = filename;
            _sequence = new List<TAG>();
            _t = new TAG(0, 0, IntPtr.Zero, false);

            _open();
        }

        private void _open()
        {
            _fs = File.Open(this._filename, FileMode.Open);

            _length = _fs.Length;
            _buffer = new byte[_fs.Length]; // allocate the size of file into buffer.

            _fs.Read(_buffer, 0, (int)_fs.Length); // read file into buffer. Starting from the beginning (0)
            _fs.Close();
        }

        private void _jmp(uint _s)
        {
            // If tag is constructed it contains elements so only jump one pos.
            if(_t._constructed)
            {
                _cp += 1;
                return;
            }

            _cp += _s;
        }

        private void _getTagNumber()
        {
            _t._flush();
            _t._constructed = Convert.ToBoolean((_buffer[_cp] >> 5) & 1); // convert that one bit to boolean. We're getting the 6th bit from 8 bits here.
            // The 6th bit is being pushed to the far left so: '00100000' will become '00000001' then & it with 00000001 to get 1(true) or 0(false)

            if (Convert.ToBoolean((_buffer[_cp] & 0x1F) == 0x1F)) // is high tag. 0x1F == 00011111 if the number also has 00011111 then this condition will evaluate to true.
            {
                while (Convert.ToBoolean((_buffer[_cp] & 0x80))) // iterate till an octet's 8th bit is not set.
                {
                    _t._type = ((_t._type << 7) | (uint)(_buffer[++_cp] & 0x7F)); // Getting the final tagnumber.
                }

                _jmp(1); // jump one position.
                return;
            }
            else if (Convert.ToBoolean((_buffer[_cp] & 0xC0)))
            {
                int _len = (_buffer[++_cp] & 0x7F); // get n amount of bytes to get the full tagnumber.
                for(int i = 0; i < _len; i++)
                {
                    _t._type = ((_t._type << 8) | (_buffer[++_cp])); // Get final tagnumber.
                }

                _jmp(1); // jump one position.
                return;
            }

            _t._type = (uint)(_buffer[_cp++] & 0x1F); // if it was neither high tag nor private tag it is a normal tag.
        }

        private void _getTagLength()
        {
            // Short form used?
            if (_buffer[_cp] <= 0x7F)
            {
                _t._length = (_buffer[_cp++]);
               unsafe
                {
                    _t._value = ((void*)_buffer[_cp]); // cast the index of datastart to the void *
                }

                _jmp(_t._length); // jump to start of new tag.
                return;
            }

            // Long form is used.
            int _len = (_buffer[_cp] & 0x7F); // get the amount of bytes that were needed to encode the full length.
            for(int i = 0; i < _len; i++)
            {
                _t._length = ((_t._length << 8) | _buffer[++_cp]); // get the final length. the '<< 8' is basically pusing the bits to the most significant side of 4 bytes.
            }
            
            _jmp(_t._length + 1); // jump to start of new tag.
        }

        public void run()
        {
            while(_cp < _length)
            {
                _getTagNumber();
                _getTagLength();

                _sequence.Add(_t);
            }
        }
    }
}
