using System;
using System.Collections.Generic;
using System.Text;

namespace NetBinSerializer {
	public interface ISerializable {
		void WriteToStream(SerializeStream stream);

		void ReadFromStream(SerializeStream stream);
	}
}
