
using System;
using System.Collections.Generic;
using System.Text;

namespace roxority_UploadZen {

	/*
meta data providers:

provider:
	connection (db, web service, local/remote file etc.)
	value getter (sql, rpc, "file methods" [content, attribute, meta data] )
	content format parser (none?, none?, xml|csv|ini|json|etc)
	content query (none?, none?, xpath/csv cell getting/etc)
value params:
	file - name, relative path, absolute path, extension, content, attribute, meta data
	sp - site title, site url, target lib name, target folder path
	user - login user name, login domain, login full name, user name, password
	datetime - now +- x
	all: 'if empty' -- prompt (auto save and suggest) or keep
	 */

	#region MetaDataConnector Class

	public abstract class MetaDataConnector {

		#region Ado Class
		#endregion

		#region File Class
		#endregion

		#region Http Class
		#endregion

	}

	#endregion

	#region MetaDataFormat Class

	public abstract class MetaDataFormat {
	}

	#endregion

	#region MetaDataParameter Class

	public abstract class MetaDataParameter {
	}

	#endregion

}
