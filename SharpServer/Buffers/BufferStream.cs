using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Encoding = System.Text.Encoding;
using StringBuilder = System.Text.StringBuilder;
using BitConverter = System.BitConverter;
using Array = System.Array;

namespace SharpServer.Buffers {
    /// <summary>
    /// Enumerator that represents the size of each datatype supported by the BufferStream class.
    /// </summary>
    public enum BufferTypeSize {
        None = 0, Bool = 1, Byte = 1, SByte = 1,
        UInt16 = 2, Int16 = 2, UInt32 = 4, Int32 = 4,
        Single = 4, Double = 8, String = -1, Bytes = -1
    }
    
    /// <summary>
    /// Provides a stream designed for reading TCP packets and UDP datagrams by type.
    /// </summary>
    public class BufferStream {
        private const byte bTrue = 1, bFalse = 0;
        private int fastAlign, fastAlignNot;
        private int iterator, length, alignment;
        private byte[] memory;
        
        /// <summary>
        /// Gets the underlying array of memory for this BufferStream.
        /// </summary>
        public byte[] Memory { get { return memory; } }
        /// <summary>
        /// Gets the length of the buffer in bytes.
        /// </summary>
        public int Length { get { return length; } }
        /// <summary>
        /// Gets the current iterator(read/write position) for this BufferStream.
        /// </summary>
        public int Iterator { get { return iterator; } }

        /// <summary>
        /// Instantiates an instance of the BufferStream class with the specified stream length and alignment.
        /// </summary>
        /// <param name="length">Length of the BufferStream in bytes.</param>
        /// <param name="alignment">Alignment of the BufferStream in bytes.</param>
        public BufferStream( int length, int alignment ) {
            fastAlign = alignment - 1;
            fastAlignNot = ~fastAlign;
            this.length = length;
            this.alignment = alignment;
            memory = new byte[ AlignedIterator( length, alignment ) ];
            iterator = 0;
        }

        /// <summary>
        /// (forced inline) Takes an iterator and aligns it to the specified alignment size.
        /// </summary>
        /// <param name="iterator">Read/write position.</param>
        /// <param name="alignment">Size in bytes to align the iterator too.</param>
        /// <returns>The aligned iterator value.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public int AlignedIterator( int iterator, int alignment ) {
            return ( ( iterator + ( alignment - 1 ) ) & ~( alignment - 1 ) );
        }

        /// <summary>
        /// (forced inline) Checks if the specified index with the specified length in bytes is within bounds of the buffer.
        /// </summary>
        /// <param name="iterator">Read/write position.</param>
        /// <param name="length">Index to check the bounds of.</param>
        /// <param name="alignment">Size in bytes to align the iterator too.</param>
        /// <returns></returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public bool IsWithinMemoryBounds( int iterator, int length, bool align = true ) {
            int iterBegin = ( align ) ? ( ( iterator + fastAlign ) & fastAlignNot ) : iterator;
            int iterEnd = iterBegin + length;
            return ( iterBegin < 0 || iterEnd >= length );
        }

        /// <summary>
        /// Allocates a new block of memory for the BufferStream with the specified length and alignment--freeing the old one if it exists.
        /// </summary>
        /// <param name="length">Length in bytes of the new block of memory.</param>
        /// <param name="alignment">Alignment in bytes to align the block of memory too.</param>
        public void Allocate( int length, int alignment ) {
            if ( memory != null ) Deallocate();

            memory = new byte[ AlignedIterator( length, alignment ) ];
            this.alignment = alignment;
            this.length = length;
        }

        /// <summary>
        /// Frees up the existing block of memory and sets the iterator to 0.
        /// </summary>
        public void Deallocate() {
            memory = null;
            iterator = 0;
        }

        /// <summary>
        /// Sets all elements in this buffer to 0.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"/>
        public void ZeroMemory() {
            Array.Clear( memory, 0, memory.Length );
        }

        /// <summary>
        /// Sets all elements in this buffer to the specified value.
        /// </summary>
        /// <param name="value">The value to zero the memory too.</param>
        public void ZeroMemory( byte value ) {
            for( int i = 0; i++ < memory.Length; ) memory[ i ] = value;
        }

        /// <summary>
        /// Creates a copy of this buffer and all it's contents.
        /// </summary>
        /// <returns>A new clone BufferStream.</returns>
        /// <exception cref="System.ArgumentNullException"/>
        public BufferStream CloneBufferStream() {
            BufferStream clone = new BufferStream( memory.Length, alignment );
            Array.Copy( memory, clone.Memory, memory.Length );
            clone.iterator = iterator;
            return clone;
        }

        /// <summary>
        /// Copies the specified number of bytes from this buffer to the destination buffer, given the start position(s) in each buffer.
        /// </summary>
        /// <param name="destBuffer">Buffer to copy the contents of this buffer too.</param>
        /// <param name="srceIndex">Start position to begin copying the data from in this buffer.</param>
        /// <param name="destIndex">Start position to begin copying the data to in the destination buffer.</param>
        /// <param name="length">Number of bytes to copy from this buffer to the destination buffer.</param>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        /// <exception cref="System.ArgumentException"/>
        public void BlockCopy( BufferStream destBuffer, int srceIndex, int destIndex, int length ) {
            Array.Copy( memory, srceIndex, destBuffer.Memory, destIndex, length );
        }

        /// <summary>
        /// Copes the specified number of bytes from this buffer to the destination buffer, given the shared start position for both buffers.
        /// </summary>
        /// <param name="destBuffer">Buffer to copy the contents of this buffer too.</param>
        /// <param name="startIndex">Shared start index of both buffers to start copying data from/to.</param>
        /// <param name="length">Number of bytes to copy from this buffer to the destination buffer.</param>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        /// <exception cref="System.ArgumentException"/>
        public void BlockCopy( BufferStream destBuffer, int startIndex, int length ) {
            Array.Copy( memory, startIndex, destBuffer.Memory, startIndex, length );
        }

        /// <summary>
        /// Copes the specified number of bytes from the start of this buffer to the start of the destination buffer.
        /// </summary>
        /// <param name="destBuffer">Buffer to copy the contents of this buffer too.</param>
        /// <param name="length">Number of bytes to copy from this buffer to the destination buffer.</param>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        /// <exception cref="System.ArgumentException"/>
        public void BlockCopy( BufferStream destBuffer, int length ) {
            Array.Copy( memory, destBuffer.Memory, length );
        }

        /// <summary>
        /// Copies the entire contents of this buffer to the destination buffer.
        /// </summary>
        /// <param name="destBuffer">Buffer to copy the contents of this buffer too.</param>
        public void BlockCopy( BufferStream destBuffer ) {
            int length = ( destBuffer.Memory.Length > memory.Length ) ? memory.Length : destBuffer.Memory.Length;
            Array.Copy( memory, destBuffer.Memory, length );
        }

        /// <summary>
        /// Resizes the block of memory for this buffer.
        /// </summary>
        /// <param name="size">Size in bytes of the resized block of memory.</param>
        public void ResizeBuffer( int size ) {
            Array.Resize<byte>( ref memory, size );
            length = memory.Length;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">BOOLEAN value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( bool value ) {
            memory[ iterator++ ] = ( value ) ? bTrue : bFalse;
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">BYTE value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( byte value ) {
            memory[ iterator++ ] = value;
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">SBYTE value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( sbyte value ) {
            memory[ iterator++ ] = (byte)value;
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">USHORT value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( ushort value ) {
            memory[ iterator++ ] = (byte)value;
            memory[ iterator++ ] = (byte)( value >> 8 );
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">SHORT value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( short value ) {
            memory[ iterator++ ] = (byte)value;
            memory[ iterator++ ] = (byte)( value >> 8 );
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">UINT value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( uint value ) {
            memory[ iterator++ ] = (byte)value;
            memory[ iterator++ ] = (byte)( value >> 8 );
            memory[ iterator++ ] = (byte)( value >> 16 );
            memory[ iterator++ ] = (byte)( value >> 24 );
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">INT value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( int value ) {
            memory[ iterator++ ] = (byte)value;
            memory[ iterator++ ] = (byte)( value >> 8 );
            memory[ iterator++ ] = (byte)( value >> 16 );
            memory[ iterator++ ] = (byte)( value >> 24 );
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">FLOAT value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( float value ) {
            byte[] bytes = BitConverter.GetBytes( value );
            for( int i = 0; i < bytes.Length; i++ ) memory[ iterator++ ] = bytes[ i ];
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">DOUBLE value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( double value ) {
            byte[] bytes = BitConverter.GetBytes( value );
            for( int i = 0; i < bytes.Length; i++ ) memory[ iterator++ ] = bytes[ i ];
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">STRING value to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( string value ) {
            byte[] bytes = Encoding.ASCII.GetBytes( (string)value );
            for( int i = 0; i < bytes.Length; i++ ) { memory[ iterator++ ] = bytes[ i ]; }
            memory[ iterator++ ] = 0;
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }
        
        /// <summary>
        /// Writes a value of the specified type to this buffer.
        /// </summary>
        /// <param name="value">BYTE[] array to be written.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Write( byte[] value ) {
            for( int i = 0; i < value.Length; i ++ ) memory[ iterator++ ] = value[ i ];
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the BOOL value in.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Read( out bool value ) {
            value = memory[ iterator++ ] >= 0;
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the BYTE value in.</param>
        /// /// <exception cref="System.IndexOutOfRangeException"/>
        public void Read( out byte value ) {
            value = memory[ iterator++ ];
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the SBYTE value in.</param>
        /// <exception cref="System.IndexOutOfRangeException"/>
        public void Read( out sbyte value ) {
            value = (sbyte)memory[ iterator++ ];
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the USHORT value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out ushort value ) {
            value = BitConverter.ToUInt16( memory, iterator );
            iterator = ( iterator + (int)BufferTypeSize.UInt16 + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the SHORT value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out short value ) {
            value = BitConverter.ToInt16( memory, iterator );
            iterator = ( iterator + (int)BufferTypeSize.Int16 + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the UINT value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out uint value ) {
            value = BitConverter.ToUInt32( memory, iterator );
            iterator = ( iterator + (int)BufferTypeSize.Int32 + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the INT value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out int value ) {
            value = BitConverter.ToInt32( memory, iterator );
            iterator = ( iterator + (int)BufferTypeSize.Int32 + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the FLOAT value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out float value ) {
            value = BitConverter.ToSingle( memory, iterator );
            iterator = ( iterator + (int)BufferTypeSize.Single + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the DOUBLE value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out double value ) {
            value = BitConverter.ToUInt16( memory, iterator );
            iterator = ( iterator + (int)BufferTypeSize.Double + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the BYTE[] value in.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out string value ) {
            StringBuilder str = new StringBuilder();
                    
            for( char c = '\0'; iterator < length; ) {
                c = (char)memory[ iterator++ ];
                if ( c == '\0' ) break;
                str.Append( c );
            }

            value = str.ToString();
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Reads the value of the specified type from the buffer.
        /// </summary>
        /// <param name="value">The OUT variable to store the BYTE[] value in.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <exception cref="System.ArgumentException"/>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.ArgumentOutOfRangeException"/>
        public void Read( out byte[] value, int length ) {
            value = new byte[ length ];
            for( int i = 0; i < length; i++ ) value[ i ] = memory[ iterator ++ ];
            iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Sets the iterator(read/write position) to the specified index, aligned to this buffer's alignment.
        /// </summary>
        /// <param name="iterator">Index to set the iterator to.</param>
        public void Seek( int iterator ) {
            this.iterator = ( iterator + fastAlign ) & fastAlignNot;
        }

        /// <summary>
        /// Sets the iterator(read/write position) to the specified index, aligned to this buffer's alignment if alignment is specified as true.
        /// </summary>
        /// <param name="iterator">Index to set the iterator to.</param>
        /// <param name="align">Whether to align the iterator or not.</param>
        public void Seek( int iterator, bool align = false ) {
            this.iterator = ( align ) ? ( iterator + fastAlign ) & fastAlignNot : iterator;
        }
    }
}
