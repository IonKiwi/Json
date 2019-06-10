using System;
using System.Collections.Generic;
using System.Text;

namespace IonKiwi.Json.MetaData {
	public interface IJsonCustomMemberTypeProvider {
		Type ProvideMemberType(string member);
	}
}
