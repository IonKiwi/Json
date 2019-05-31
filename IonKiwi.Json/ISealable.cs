using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json {
	public interface ISealable {
		void Seal();
		bool IsSealed { get; }
	}
}
