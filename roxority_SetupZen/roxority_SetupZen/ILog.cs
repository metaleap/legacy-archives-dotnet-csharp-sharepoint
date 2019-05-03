
using System;
using System.Collections.Generic;
using System.Text;

namespace roxority_SetupZen {

	public interface ILog {

		void Error (object message);
		void Error (object message, Exception t);
		void Fatal (object message);
		void Fatal (object message, Exception t);
		void Info (object message);
		void Info (object message, Exception t);
		void Warn (object message);
		void Warn (object message, Exception t);

	}

}
