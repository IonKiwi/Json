using System;

namespace IonKiwi.Json.MetaData {
	public class JsonMetaDataEventArgs : EventArgs {

		public JsonMetaDataEventArgs() {

		}

		public void IsCollection() { }

		public void IsDictionary() { }

		public void IsObject() { }

		public void AddProperty<T>(string name, Action<T> setter) {

		}
	}
}