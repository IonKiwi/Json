#region License
// Copyright (c) 2019 Ewout van der Linden
// https://github.com/IonKiwi/Json/blob/master/LICENSE
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

#if NET472
using PlatformTask = System.Threading.Tasks.Task<bool>;
#else
using PlatformTask = System.Threading.Tasks.ValueTask<bool>;
#endif

namespace IonKiwi.Json.MetaData {
	public interface IJsonReadMemberProvider {
		bool ReadMember(JsonReadMemberProviderContext context);
		PlatformTask ReadMemberAsync(JsonReadMemberProviderContext context);
	}
}
