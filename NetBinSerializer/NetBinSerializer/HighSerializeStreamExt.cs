using System;
using System.Collections.Generic;
using System.Text;

namespace NetBinSerializer {
	public static class HighSerializeStreamExt {

		public static HighSerializeStream GetStream(this Serializer serializer) => new HighSerializeStream(serializer);

		public static HighSerializeStream GetStream(this Serializer serializer, byte[] bytes) => new HighSerializeStream(serializer, bytes);

	}
}
