#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

#if NET472
using PlatformTaskBool = System.Threading.Tasks.Task<bool>;
#else
using PlatformTaskBool = System.Threading.Tasks.ValueTask<bool>;
#endif

namespace IonKiwi.Json.MetaData {
	public interface IJsonWriteMemberProvider {
		bool WriteMember(JsonWriteMemberProviderContext context);
		PlatformTaskBool WriteMemberAsync(JsonWriteMemberProviderContext context);
	}
}
