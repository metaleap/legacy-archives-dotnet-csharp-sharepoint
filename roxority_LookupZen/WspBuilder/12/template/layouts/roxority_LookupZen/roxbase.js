
var $J = jQuery.noConflict();

var $B = {

	/*
	 * Automatically called at the end of this script file.
	 */
	__Init: function() {
		var obj, tmp, initializing = false, fnTest = /xyz/.test(function() {xyz;}) ? /\b_base\b/ : /.*/;
		// Set up class inheritance support, inspired by and ripped off John Resig
		$B.Meta.__.__Extend = function(subType) {
			var _prot = this.prototype, ctor = function () {
				// Dummy class constructor --- all construction is actually done in the init method
				if ((!initializing) && this.init) this.init.apply(this, arguments);
			}, createOverride = function(name, fn) {
				return function() {
					var tmp = this._base;
					// Add a new ._base() method that is the same method but on the super-class
					this._base = _prot[name];
					// The method only need to be bound temporarily, so we remove it when we're done executing
					try { return fn.apply(this, arguments); } finally { this._base = tmp; }
				};
			};
			// Instantiate a base class (but only create the instance, don't run the init constructor)
			initializing = true; var prot = new this(); initializing = false;
			// Copy the properties over onto the new prototype
			for (var name in subType)
				// Check if we're overwriting an existing function
				if (typeof subType[name] == 'function' && (typeof _prot[name]) == 'function' && fnTest.test(subType[name])) {
					//$M.log('override: ' + name);
					prot[name] = createOverride(name, subType[name]);
				} else
					prot[name] = subType[name];
			ctor.prototype = prot; // Populate our constructed prototype object
			ctor.constructor = ctor; // Enforce the constructor to be what we expect
			ctor.__Extend = arguments.callee; // And make this class extendable
			return ctor;
		};
		// Create wrapper classes for namespace-objects in $B
		for (var p in $B)
			if ($B.Meta.IsObject(obj = $B[p]) && obj != $B.Meta && obj.$ == undefined) {
				tmp = obj;
				obj = $B[p] = $B.Meta.CreateWrapperType(obj);
				for(var p in tmp)
					obj[p] = tmp[p];
			}
	},

	Array: {

		AddRange: function(arr, arr2) {
			for (var i = 0; i < arr2.length; i++)
				arr[arr.length] = arr2[i];
			return arr;
		},

		/*
		 * Create a shallow clone of the specified array (or object with a
		 * numeric .length attribute). The return value is always an array.
		 */
		Clone: function(arr) {
			var a = [];
			for (var i = 0, l = (a.length = arr.length); i < l; i++) a[i] = arr[i];
			return a;
		},

		/*
		 * Returns true if the index of val in arr is 0 or greater; otherwise,
		 * returns false.
		 */
		Contains: function(arr, val) {
			return (arr ? ($J.inArray(val, arr) >= 0) : false);
		},

		/*
		 * Contains true if the index of obj in arr is 0 or greater (using the
		 * identity === instead of equality == test); otherwise, returns false.
		 */
		ContainsID: function(arr, obj) {
			return this.IndexOfID(arr, obj) >= 0;
		},

		/*
		 * Returns true if an element exists in arr for which pred, when passed
		 * the element, returns a true-equivalent value; otherwise, returns
		 * false.
		 */
		Exists: function(arr, pred) {
			return (this.IndexOf(arr, pred) >= 0);
		},

		/*
		 * Returns the first "value" (i.e. defined and non-null) element in arr,
		 * if any (starting at startIndex or 0).
		 */
		FirstValue: function(arr, startIndex) {
			if (!startIndex) startIndex = 0;
			if (arr && arr.length) for (var i = startIndex, l = arr.length; i < l; i++) if (arr[i] != null && arr[i] != undefined) return arr[i];
		},

		/*
		 * Returns the index of the first element in arr for which pred, when
		 * passed the element, returns a true-equivalent value.
		 */
		IndexOf: function(arr, pred) {
			if (arr && arr.length && pred)
				for (var i = 0, l = arr.length; i < l; i++)
					if (pred(arr[i]))
						return i;
			return -1;
		},

		/*
		 * Returns the index of the first occurrence of obj in arr, or -1 if arr
		 * does not contain obj (using the identity === instead of equality ==
		 * test)
		 */
		IndexOfID: function(arr, obj) {
			if (arr && arr.length)
				for (var i = 0, l = arr.length; i < l; i++)
					if (arr[i] === obj)
						return i;
			return -1;
		},

		/*
		 * Inserts value into arr at the position specified by startIndex. If
		 * clone is passed and true-equivalent, returns a shallow clone of arr
		 * containing value at startIndex; otherwise, modifies arr destructively
		 * in-place and returns it.
		 */
		Insert: function(arr, startIndex, value, clone) {
			if (clone)
				arr = this.Clone(arr);
			arr.length++;
			for (var l = arr.length, i = (l - 1); i > startIndex; i--)
				arr[i] = arr[i - 1];
			arr[startIndex] = value;
			return arr;
		},

		/*
		 * Non-recursively and destructively merges objects in the specified
		 * 'source' array into the specified 'target' array. Both arrays should
		 * not contain any null or undefined elements. Objects in both arrays
		 * should have an attribute (specified by the 'idProp' argument) that
		 * uniquely identifies the object instance so as to properly match
		 * 'source' and 'target' objects. If the 'added' array is passed, items
		 * in 'source' that are missing in 'target' (per the 'idProp' attribute)
		 * are added to both the 'target' and 'added' arrays. If the 'removed'
		 * array is passed, items in 'target' that are missing in 'source' (per
		 * the 'idProp' attribute) are removed from 'target' and added to
		 * 'removed'. Notes: - objects *added* to 'target' are identical to the
		 * instances 'source'; however, objects that existed in 'target'
		 * previously only have the same ['idProp'] value, to be synced/merged --
		 * if desired -- by the calling code. - non-recursive: the calling code
		 * is responsible for also merging target and source sub-objects.
		 */
		MergeObjects: function(target, source, idProp, added, removed) {
			var ls = source ? source.length : 0, lt = target.length, c = 0, tmp, r = true;
			// find items in source to be added to target
			if (added)
				for (var is = 0; is < ls; is++)
					if (($B.Meta.IsObject(source[is])) && (!this.Exists(target, function(obj) { return obj[idProp] == source[is][idProp]; })))
						added.push(source[is]);
			// find items not in source, to be removed from target
			if (removed) {
				for (var it = 0; it < lt; it++)
					if (!this.Exists(source, function(obj) { return (obj[idProp] ? obj[idProp] : obj) == target[it][idProp]; })) {
						removed.push(target[it]);
						target[it] = null;
					}
				// shrink target to get rid of removed (empty) slots
				for (var it = 0; it < lt; it++) {
					if (!target[it])
						c++;
					if ((c) && ((it + c) < target.length))
						target[it] = target[it + c];
					if ((!target[it]) && (this.FirstValue(target, it)))
						it--;
				}
			}
			if (c)
				target.length = lt - c;
			// append to target items to be added
			if (added)
				for (var ia = 0, la = added.length; ia < la; ia++)
					target.push(added[ia]);
			// fix order of items now that target only contains items that "are"
			// also in source
			while (r) {
				r = false;
				for (var it = 0; it < ls; it++)
					if (target[it][idProp] != (source[it][idProp] ? source[it][idProp] : source[it])) {
					// find current index in target thats to be moved to it
						if ((c = this.IndexOf(target, function(obj) { return obj[idProp] == (source[it][idProp] ? source[it][idProp] : source[it]); })) < it)
							r = true;
						tmp = target[it];
						target[it] = target[c];
						target[c] = tmp;
						if (r)
							break;
					}
			}
		}

	},

	DateTime: {

		/*
		 * Returns the value returned by JavaScript's "new Date().getTime()"
		 */
		Ticks: function() {
			return new Date().getTime();
		}

	},

	Math: {

		/*
		 * Returns a number between 0 and 100 (both inclusive) indicating how
		 * much percent 'val' is of 'hundredVal'.
		 */
		Percent: function(val, hundredVal) {
			return (hundredVal ? ((100 / hundredVal) * val) : 0);
		},

		/*
		 * Returns 'percentVal' percent of 'val'.
		 */
		Percentage: function(val, percentVal) {
			return (val / 100) * percentVal;
		},

		Random: function() {
			return Math.random();
		}

	},

	Meta: {

		__: function() {
		},

		/*
		 * Creates a class that maintains a '__value' (initially passed to its
		 * constructor) and that for each function in 'proto', provides a method
		 * calling that function with both its own '__value' and any further
		 * arguments passed to the method call. Example:
		 * $B.Text.EndsWith('foobar', 'bar') is equivalent to: new
		 * $B.Text.$('foobar').EndsWith('bar') where: $B.Text.$ is the wrapper
		 * class that during $B.__Init() was created by
		 * $B.Meta.CreateWrapperType($B.Text)
		 */
		CreateWrapperType: function(proto, baseType) {
			var fn, cls = { init: function(value) { this.__value = value; } },
				creator = function(fn, proto) { return function() { return fn.apply(proto, $B.Array.Insert(arguments, 0, this.__value, true)); }; };
			for (var p in proto) if ((typeof (fn = proto[p])) == 'function') cls[p] = creator(fn, proto);
			return ((baseType && baseType.__Extend) ? baseType : $B.Meta.__).__Extend(cls);
		},

		/*
		 * Returns a function that calls 'self'.'fn' with the specified 'args'.
		 */
		CreateMethod: function(self, fn, args) {
			return (function(self) { return function() { return fn.apply(self, args); } })(self);
		},

		Clone: function(obj, except) {
			var nu = {};
			for (var p in obj)
				if (!(except && $B.Array.Contains(except, p)))
					nu[p] = obj[p];
			return nu;
		},

		Delete: function(obj) {
			if (arguments && (arguments.length > 1))
				for (var i = 1; i < arguments.length; i++)
					delete obj[arguments[i]];
			return obj;
		},

		/*
		 * Returns true if v.constructor equals Array; otherwise, returns false.
		 */
		IsArray: function(v) {
			return v.constructor == Array;
		},

		/*
		 * Returns true if 'obj' is an object equal to '{}', that is, without any attributes whatsoever; otherwise, returns false.
		 */
		IsEmptyObject: function(obj) {
			if (!this.IsObject(obj))
				return false;
			for (var p in obj)
				return false;
			return true;
		},

		/*
		 * Returns true if 'obj' is an object; otherwise, returns false.
		 */
		IsObject: function(obj) {
			return (((typeof obj) == 'object') && (obj != null));
		},

		/*
		 * Returns true if 'v' is a string; otherwise, returns false.
		 */
		IsString: function(v) {
			return ((typeof v) == 'string');
		},

		/*
		 * Sets the attributes specified by the arguments 1, 3, 5 and so on to
		 * the values specified by the arguments 2, 4, 6 and so on, for the
		 * object specified by the (first) argument 0. If this is omitted,
		 * undefined or null, returns a new object with the specified attributes
		 * and values.
		 */
		Set: function(obj, name, value) {
			if (arguments.length == 2)
				return this.Set(null, obj, name);
			if (obj && !this.IsObject(obj))
				return this.set.apply(this, $B.Array.Insert(arguments, 0, null, true));
			if (!obj)
				obj = {};
			for (var i = 1; i < (arguments.length - 2); i++) {
				if (!obj[arguments[i]])
					obj[arguments[i]] = {};
				obj = obj[arguments[i]];
			}
			obj[arguments[arguments.length - 2]] = arguments[arguments.length - 1];
			return obj;
		},

		/*
		 * Sets the boolean attribute specified by 'prop' in 'target' to either
		 * true or false, depending on the value of the attribute in 'source'.
		 * If 'trueOnly' is a true-equivalent value, only performs an action if
		 * the 'prop' attribute of 'target' would be set to true.
		 */
		SyncBool: function(target, source, prop, trueOnly) {
			if (source[prop] && (!target[prop]))
				target[prop] = true;
			else if ((!source[prop]) && (!trueOnly) && target[prop])
				target[prop] = false;
		},

		/*
		 * Returns true if v is a true-equivalent value; otherwise, returns
		 * false.
		 */
		ToBool: function(v) {
			return (v ? true : false);
		},

		/*
		 * Returns the JSON representation of 'obj', excluding the attributes
		 * specified in the 'exclude' array, and optionally escaping the
		 * resulting JSON representation for HTML display, if 'escapeForHtml' is
		 * a true-equivalent value. The 'objects' array does not have to be
		 * passed and is used internally to deal with recursion: objects
		 * contained in the array are not included in the resulting JSON
		 * representation.
		 */
		ToString: function(obj, escapeForHtml, exclude, objects) {
			var type = (typeof obj), list;
			if (!objects) objects = [];
			if (!exclude) exclude = [];
			if (type == 'string')
				return '"' + $B.Text.Replace(obj, '"', '\\"') + '"';
			else if (type != 'object')
				return '' + obj;
			else if (!obj)
				return 'null';
			if ($B.Array.ContainsID(objects, obj))
				return '"#REF"';
			else
				objects.push(obj);
			list = [];
			if ($B.Meta.IsArray(obj)) {
				for (var i = 0, l = obj.length; i < l; i++)
					list.push(this.ToString(obj[i], escapeForHtml, exclude, objects));
				return '[' + list.join(',') + ']';
			} else {
				for (var prop in obj)
					if (!$B.Array.Contains(exclude, prop))
						list.push('"' + prop + '":' + this.ToString(obj[prop], escapeForHtml, exclude, objects));
				return '{' + list.join(',') + '}';
			}
		},

		/*
		 * TODO
		 */
		TypeName: function(arg) {
			var pos, cn = { namespace: 'core', name: '', fullname: '' };
			if ((typeof arg) != 'string')
				return arg.namespace + '/' + arg.name;
			if ((pos = arg.lastIndexOf('/')) > 0) {
				cn.namespace = arg.substr(0, pos);
				cn.name = arg.substr(pos + 1);
			} else
				cn.name = arg;
			cn.safename = $B.Text.Safe(arg);
			return cn;
		}

	},

	Resources: {

		__Get: function(prefix, resName, args) {
			var res;
			if (arguments.length > 3)
				return this.__Get.apply(this, $B.Meta.IsString(resName) ? ([prefix, resName, arguments.slice(2)]) : (null, prefix, arguments.slice(1)));
			if (!$B.Meta.IsString(resName))
				return this.__Get.apply(this, $B.Array.Insert(arguments, 0, null, true));
			if (arguments.length == 2 && $B.Meta.IsArray(arguments[1]))
				return this.__Get('_', arguments[0], arguments[1]);
			if (!resName) { resName = prefix; prefix = '_'; } else if (!prefix) prefix = '_';
			res = (($B.Resources[prefix] && $B.Resources[prefix][resName]) ? $B.Resources[prefix][resName] : resName);
			return (args ? $B.Text.Format(res, args) : res);
		},

		__Update: function(prefix, resources) {
			if (prefix == '__Update' || prefix == '__Get') throw $B.Resources.__Get('error_id_invalid', '__Update', ['prefix', prefix]);
			if (!$B.Resources[prefix]) $B.Resources[prefix] = {};
			for (var p in resources)
				$B.Resources[prefix][p] = resources[p];
			return $B.Resources[prefix];
		}

	},

	Text: {

		_regexCache: {},

		/*
		 * Returns true of 'str' ends with 'val'; otherwise, returns false.
		 */
		EndsWith: function(str, val) {
			return this.SubstrEquals(str, val, str.length - val.length);
		},

		/*
		 * Returns 'str' with all occurrences of '{0}', '{1}', '{2}' and so on
		 * replaced with the corresponding arguments passed to the function.
		 */
		Format: function(str) {
			var args = ((arguments.length == 2 && $B.Meta.IsArray(arguments[1])) ? arguments[1] : arguments);
			for (var i = ((args == arguments) ? 1 : 0), l = args.length; i < l; i++)
				str = str.replace(this.RegEx("\\{" + ((args == arguments) ? (i - 1) : i) + "\\}", "g"), '' + args[i]);
			return str;
		},

		/*
		 * Returns a cached RegExp instance representing the specified regular
		 * expression initialized with the specified options.
		 */
		RegEx: function(regex, options) {
			var key = regex + '_-/\-_' + options, rex;
			if (!(rex = this._regexCache[key]))
				this._regexCache[key] = rex = new RegExp(regex, options);
			return rex;
		},

		/*
		 * Returns 'str' with all matching occurrences specified by the 'rex'
		 * regular expression replaced with 'replacement'.
		 */
		Replace: function(str, rex, replacement) {
			var isArr;
			for (var i = 1, l = arguments.length; i < l; i += 2) {
				rex = arguments[i];
				replacement = arguments[i + 1];
				str = (str + '').replace(this.RegEx((isArr = ($B.Meta.IsArray(rex) && rex.length >= 2)) ? rex[0] : rex, (isArr ? rex[1] : 'g') || 'g'), replacement);
			}
			return str;
		},

		/*
		 * Returns 'value' with all characters other than [0-9], [a-z] and [A-Z]
		 * replaced with 'safeChar' (or '_'). If 'keepLength' is passed and a
		 * true-equivalent value, subsequently occurring duplicates of
		 * 'safeChar' are permitted in the return value, otherwise, they will be
		 * reduced to just one occurrence.
		 */
		Safe: function(value, safeChar, keepLength) {
			var s = '', c, lastChar = ' ';
			if (!safeChar) safeChar = '_';
			if (!$B.Meta.IsString(value))
				value = '' + value;
			for (var i = 0, l = value.length; i < l; i++)
				s += (lastChar = (((((c = value[i]) >= '0') && (c <= '9')) || ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z'))) ? c : (((!keepLength) && (lastChar == safeChar || lastChar == '')) ? '' : safeChar)));
			return s;
		},

		/*
		 * Returns true of 'str' starts with 'val'; otherwise, returns false.
		 */
		StartsWith: function(str, val) {
			return this.SubstrEquals(str, val, 0, val.length);
		},

		/*
		 * Returns true if the sub-string in 'str' that starts at 'substrStart'
		 * end ends at 'substrEnd' equals 'val'; otherwise, returns false.
		 */
		SubstrEquals: function(str, val, substrStart, substrEnd) {
			if ((!val) && (!str))
				return true;
			if ((!val) || (!str) || (!val.length) || (!str.length) || (str.length < val.length))
				return false;
			return ((!substrEnd) ? (str.substr(substrStart)) : (str.substr(substrStart, substrEnd))) == val;
		}

	},

	UI: {

		Anim: {

			counter: 0,
			enabled: true,
			duration: 500,

			__coreAnim: function($dom, method, cssProp, cssVal, callback) {
				if (this.enabled) {
					$B.UI.Anim.counter++;
					return $dom[method](this.duration, this.__createCallback(callback));
				} else {
					$dom.css(cssProp, cssVal);
					if (callback)
						callback();
					return $dom;
				}
			},

			__createCallback: function(cb) {
				return function() { $B.UI.Anim.counter--; if (cb) cb(); };
			},

			Animate: function($dom, cssProps, callback, options) {
				if(!options)
					options = {};
				if(!options.queue)
					options.queue = false;
				if(!callback)
					callback = options.complete;
				else if(options.complete)
					callback = (function(cb1, cb2) { return function(){ if (cb2) cb2(); if (cb1) cb1(); }; })(options.complete, callback);
				if(!options.duration)
					options.duration = this.duration;
				if (this.enabled) {
					options.complete = this.__createCallback(callback);
					$B.UI.Anim.counter++;
					return $dom.animate(cssProps, options);
				} else {
					for (var p in cssProps)
						$dom.css(p, cssProps[p]);
					if (callback)
						callback();
					return $dom;
				}
			},

			FadeIn: function($dom, callback) {
				return this.__coreAnim($dom, 'fadeIn', 'display', 'block', callback);
			},

			FadeOut: function($dom, callback) {
				return this.__coreAnim($dom, 'fadeOut', 'display', 'none', callback);
			},

			ScrollTo: function($dom, target, callback) {
				$B.UI.Anim.counter++;
				return $dom.scrollTo(target, this.enabled ? this.duration : 1, this.__createCallback(callback));
			},

			SlideDown: function() {
				return this.__coreAnim($dom, 'slideDown', 'display', 'none', callback);
			},

			SlideToggle: function($dom, callback) {
				return this.__coreAnim($dom, 'slideToggle', 'display', ($dom.css('display') == 'none') ? 'block' : 'none', callback);
			},

			SlideUp: function($dom, callback) {
				return this.__coreAnim($dom, 'slideUp', 'display', 'none', callback);
			}

		},

		/*
		 * Contains functions and/or objects that can be invoked by JSON
		 * preprocessor instructions being processed by the UI.PreprocessJson
		 * method.
		 */
		JsonPreprocessors: {

			res: function(v) {
				var pos = v.indexOf(':');
				return $B.Resources.__Get(pos ? v.substr(0, pos) : null, pos ? v.substr(pos + 1) : v);
			}

		},

		/*
		 * Traverses the entire specified 'json' object graph and processes all
		 * attributes whose names start with '$' according to the following
		 * rules: If the value of the attribute is a string of the form
		 * 'yy:zzz', and if the UI.JsonPreprocessors hash object has an
		 * attribute with the name 'yy', then the '$x' attribute is removed from
		 * the object and a new attribute 'x' is added and its value set: - if
		 * UI.JsonPreprocessors.yy is a function, to its return value when
		 * called with argument 'zzz' - if UI.JsonPreprocessors.yy is an object,
		 * to the value of its 'zzz' attribute - otherwise, to the value of
		 * UI.JsonPreprocessors.yy - or, if an error occurs during this process
		 * (for example, while calling .yy-the-function), to the error message
		 * unless ignoreErrors is a true-equivalent value. If 'json' or a
		 * sub-object has an '.options.$templates' object, this is removed and
		 * the templates that it specifies are passed for further processing to
		 * UI.PreprocessJsonTemplates.
		 */
		PreprocessJson: function(json, ignoreErrors) {
			var pos, val, cmd, handler, temps, t;
			// process $templates first because the $ attribute would otherwise
			// be processed and replaced by the code that follows further down
			if (json.options && (temps = json.options.$templates)) {
				delete json.options.$templates;
				for (var x in json)
					json[x] = this.PreprocessJsonTemplates(json[x], temps, true);
			}
			// process all other $x attributes
			for (var p in json)
				if (p && p.length && p.length > 1 && (p.substr(0, 1) == '$') && ((typeof (val = json[p])) == 'string') && ((pos = val.indexOf(':')) > 0) && (handler = this.JsonPreprocessors[cmd = val.substr(0, pos)] ))
					try {
						json[p.substr(1)] = (((t = (typeof handler)) == 'function') ? (handler(val.substr(pos + 1))) : ((t == 'object') ? (handler[val.substr(pos + 1)]) : (handler)));
					} catch(e) {
						json[p.substr(1)] = (ignoreErrors ? null : e);
					} finally {
						delete json[p];
					}
				else if ((typeof json[p]) == 'object')
					json[p] = this.PreprocessJson(json[p], ignoreErrors);
			return json;
		},

		/*
		 * If the specified 'json' object has an attribute '$template_id' and
		 * the specified 'temps' object contains an object with that attribute
		 * name, merges all attributes and values of the template object into
		 * the 'json' object if either 'overwrite' is passed and has a
		 * true-equivalent value or Recurses into sub-objects unless
		 * 'dontRecurse' is passed and has a true-equivalent value. Returns the
		 * specified 'json' object.
		 */
		PreprocessJsonTemplates: function(json, temps, dontRecurse, overwrite) {
			var jp, t;
			if (($B.Meta.IsArray(json))) {
				for (var i = 0, l = json.length; i < l; i++) if (!dontRecurse) json[i] = this.PreprocessJsonTemplates(json[i], temps);
			} else if ($B.Meta.IsObject(json))
				for (var p in json)
					if ((p != '$template_id') || (!(t = temps[jp = json[p]]))) {
						if (!dontRecurse) json[p] = this.PreprocessJsonTemplates(json[p], temps);
					} else {
						delete json[p]; for (var pt in t) if (overwrite || (!json[pt])) json[pt] = t[pt];
					}
			return json;
		}

	},

	Util: {

		In: function(v, arr) {
			return $B.Array.Contains(arr, v);
		}

	},

	Web: {

		/*
		 * Returns 'v' with all occurrences of the '&', '<' and '>' characters HTML-escaped,
		 * all line-break characters (\n) replaced with a <br/> tag, all tab
		 * characters (\t) replaced with four non-breaking spaces, and every other
		 * white-space character replaced with a non-breaking space.
		 */
		EscapeForHtml: function(v, nbsp) {
			var result = $B.Text.Replace(v, '&', '&amp;', '<', '&lt;', '>', '&gt;', '\\n', '<br/>', '\\t', '&nbsp;&nbsp;&nbsp;&nbsp;');
			return nbsp ? $B.Text.Replace(result, '\\s', '&nbsp;') : result;
		}

	}

};

$B.__Init();
