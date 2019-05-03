var roxUps = {}, roxUpLoc = {}, roxExtIcons = {}, roxExtIconsDone = false, roxLocDone = false, roxBlocked = [], roxUpFlags = 0, roxExInt = null, roxFsKb = 1024, roxFsMb = 1024 * 1024, roxFsGb = 1024 * 1024 * 1024, roxFnLen = 40;

function roxAlertBlockedInfo() {
	alert(roxUpLoc['BlockInfo'] + '\n\n' + roxBlocked.join(' · '));
}

function roxCheckExists() {
	for (var rup in roxUps)
		roxUps[rup].checkExists();
}

function roxFileName(fn, isZip, zc, file, ctlID, webUrl) {
	var l = isZip ? (roxFnLen - ((zc > 1) ? 12 : 10)) : roxFnLen, r = fn;
	if (fn && fn.length && (fn.length > l))
		r = '<span title="' + fn + '">' + fn.substr(0, l / 2) + ' &hellip; ' + fn.substr(fn.length - (l / 2)) + '</span>';
	if (isZip) {
		r += ('&nbsp;<select class="rox-uplzip roxup-notonupload" onchange="roxUps[\'' + ctlID + '\'].onPropChange(\'' + file.id + '\', \'unzip\', this.options[this.selectedIndex].value);"><option value="0">' + roxUpLoc['ZipExtract0'] + '</option><option value="1"' + ((file.unzip == '1') ? ' selected="selected"' : '') + '>' + roxUpLoc['ZipExtract1'] + '</option><option value="2"' + ((file.unzip == '2') ? ' selected="selected"' : '') + '>' + roxUpLoc['ZipExtract2'] + '</option></select>');
		if (zc > 1)
			r += ' <a class="roxup-notonupload roxup-onhover" href="#noop' + file.id + '" onclick="roxUps[\'' + ctlID + '\'].cloneProp(\'' + file.id + '\', \'unzip\');"><img border="0" src="' + webUrl + '/_layouts/images/checkall.gif" alt="' + roxUpLoc['SetAllZip'] + '" title="' + roxUpLoc['SetAllZip'] + '"/></a>';
	}
	return r;
}

function roxFileSize(size) {
	var s;
	if (size) {
		if (size >= roxFsGb)
			s = (size / roxFsGb).toFixed(2) + ' GB';
		else if (size >= roxFsMb)
			s = (size / roxFsMb).toFixed(2) + ' MB';
		else if (size >= roxFsKb)
			s = (size / roxFsKb).toFixed(2) + ' KB';
		else
			s = size + ' B';
		return '<div class="rox-uplfilesize">' + s + '</div>';
	}
	return '';
}

function roxNewPlup(ctlID, webUrl, appPath, checkInOut, rt, dd, nodd, removeBlocked, extFilters, folderPath, repl, hasHelp, unique, overwrite, maxSize, resize, chunkSize, defCheckIn) {
	var plup, plobj = {
		"runtimes": 'html5,' + ( ((rt == 'f') ? 'flash' : ((rt == 's') ? 'silverlight' : ((rt == 'fs') ? 'flash,silverlight' : 'silverlight,flash'))) ) + ',html4',
		"browse_button": ctlID + "_roxnew",
		"container": ctlID,
		"url": webUrl + "/_layouts/roxority_UploadZen/uploadzen.aspx?js=1&ow=" + (overwrite ? 1 : 0) + "&co=" + (checkInOut ? 1 : 0) + "&cid=" + ctlID + "&dn=" + folderPath + "&rp=" + repl + "&hh=" + (hasHelp ? 1 : 0) + "&",
		"multipart": false, "multi_selection": true,
		"flash_swf_url": appPath + "/plup.swf",
		/*"drop_element": ctlID + "_fieldset",*/
		"silverlight_xap_url": appPath + "/plup.xap",
		"silverlight_bgcolor": "#f0f0f0",
		"silverlight_text": dd,
		"silverlight_color": "#0072BC"
	};
	if (extFilters)
		plobj["filters"] = extFilters;
	if (unique)
		plobj["unique_names"] = true;
	if (maxSize)
		plobj["max_file_size"] = maxSize;
	if (resize)
		plobj["resize"] = resize;
	if (chunkSize)
		plobj["chunk_size"] = chunkSize;
	plobj["ourl"] = plobj.url;
	plup = new plupload.Uploader(plobj);
	plup.bind('BeforeUpload', function(plup, file) {
		var uz = '0', ci = '', roxUp = roxUps[ctlID];
		if (roxUp)
			for (var i = 0; i < roxUp.files.length; i++)
				if (roxUp.files[i].id == file.id) {
					uz = roxUp.files[i].unzip;
					ci = roxUp.files[i].checkin;
					break;
				}
		plup.settings.url = plup.settings.ourl + 'uz=' + uz + '&ci=' + ci + '&';
		plup.refresh();
	});
	plup.bind('Error', function(plup, err) {
		var roxUp = roxUps[ctlID], file = (err ? err.file : null);
		if (roxUp && file) {
			if (err['code'] && ((err.code == -601) || (err.code == -600)))
				roxUp.remove(file.id);
			else {
				for (var i = 0; i < roxUp.files.length; i++)
					if (roxUp.files[i].id == file.id) {
						roxUp.refreshStatus(i, roxUpLoc['Messages'] + '\\n' + file.name + ':\\n\\n' + err.message);
						break;
					}
				roxUp.refresh(true);
			}
		} else if ((err.code + '') != '-500')
			alert('Error code #' + err.code + ': ' + err.message);
		plup.refresh();
	});
	plup.bind('Init', function(plup, params) {
		if (((plup.runtime = params.runtime) != 'silverlight') && dd)
			jQuery('#' + ctlID + '_roxnew').text(nodd);
		if ((plup.runtime == 'silverlight') || (plup.runtime == 'flash') || (plup.runtime == 'html5') || (plup.runtime == 'html4')) {
			jQuery('#' + ctlID + '_runtime').text(plup.runtime.substr(0, 1).toUpperCase() + plup.runtime.substr(1));
			jQuery('#' + ctlID + '_noruntime').css({ "display": "none" });
			jQuery('#' + ctlID + '_roxnew').css({ "visibility": "visible" });
			jQuery('#' + ctlID + '_nofiles').css({ "visibility": "visible" });
			try {
				plup.refresh();
			} catch (err) {
			}
		}
	});
	plup.bind('FilesAdded', function(plup, files) {
		var up = roxUps[ctlID], found, now = new Date().getTime(), ext, pos;
		if ((!plup['lastAdd']) || ((now - plup.lastAdd) > 1000)) {
			plup.lastAdd = now;
			for (var i = 0; i < files.length; i++) {
				found = null;
				ext = (((pos = files[i].name.lastIndexOf('.')) > 0) ? files[i].name.substr(pos + 1) : '');
				for (var j = 0; j < up.files.length; j++)
					if ((up.files[j].id == files[i].id) || (up.files[j].name == files[i].name)) {
						found = up.files[j];
						break;
					}
				if ((!ext) || (jQuery.inArray(ext, roxBlocked) < 0) || !removeBlocked)
					if (!found)
						up.files.push({ id: files[i].id, status: files[i].status, unzip: null, name: files[i].name, checkin: defCheckIn, size: files[i].size, namechecked: false });
					else {
						found.status = files[i].status;
						found.name = files[i].name;
						found.size = files[i].size;
					}
			}
			up.refresh();
		} else
			for (var i = 0; i < files.length; i++)
				roxUps[ctlID].plup.removeFile(files[i]);
	});
	plup.bind('FileUploaded', function(plup, file, resp) {
		var roxUp = roxUps[ctlID];
		if (resp && resp['response'])
			plup.trigger('Error', { "file": file, "message": resp.response });
		else {
			if (roxUp && file) {
				for (var i = 0; i < roxUp.files.length; i++)
					if (roxUp.files[i].id == file.id) {
						roxUp.refreshStatus(i, plupload.DONE);
						break;
					}
				roxUp.refresh(true);
			}
			plup.refresh();
		}
	});
	plup.bind('StateChanged', function(plup) {
		var roxUp = roxUps[ctlID];
		if (roxUp && plup) {
			roxUp.uploading = (((plup.state + '') == '2') ? 1 : 0);
			roxUp.refresh(true);
			plup.refresh();
		}
	});
	plup.bind('UploadProgress', function(plup, file) {
		var roxUp = roxUps[ctlID];
		if (roxUp && file && file.percent && (file.percent < 100))
			for (var i = 0; i < roxUp.files.length; i++)
				if (roxUp.files[i].id == file.id) {
					roxUp.refreshStatus(i, plupload.UPLOADING, file);
					break;
				}
		plup.refresh();
	});
	plup.bind('UploadFile', function(plup, file) {
		plup.refresh();
	});
	plup.init();
	return plup;
}

function roxNewUpper(id, ctlID, webUrl, checkInOut, hasHelp, repl, folderPath, numInputs, isChrome, appPath, rt, dd, nodd, removeBlocked, extFilters, libID, allowUnzip, unique, overwrite, maxSize, resize, chunkSize, defCheckIn, canChangeCheckIn) {
	//alert(webUrl + '\n' + folderPath);
	var tmp, up = {
		$ctl: jQuery(tmp = document.getElementById(ctlID)),
		ctl: tmp,
		checkingExist: false,
		id: id,
		existFiles: [],
		files: [],
		uploading: 0,
		plup: roxNewPlup(ctlID, webUrl, appPath, checkInOut, rt, dd, nodd, removeBlocked, extFilters, folderPath, repl, hasHelp, unique, overwrite, maxSize, resize, chunkSize, defCheckIn),

		cancel: function(fileID) {
			if (this.plup) {
				this.plup.stop();
				this.uploading = 0;
			}
			for (var i = 0; i < this.files.length; i++)
				if (((!fileID) || (this.files[i].id == fileID)) && ((this.files[i].status == plupload.QUEUED) || (this.files[i].status == plupload.UPLOADING))) {
					if (this.uploading)
						this.uploading--;
					this.refreshStatus(i, -1);
				}
			return this.refresh(true);
		},

		checkExists: function() {
			var url, fileNames = [], pos;
			if (!this.checkingExist) {
				this.checkingExist = true;
				for (var i = 0; i < this.files.length; i++)
					if ((jQuery.inArray(this.files[i].name, this.existFiles) < 0) && (jQuery.inArray(this.files[i].name, fileNames) < 0) && ((!this.files[i].unzip) || (this.files[i].unzip == '0')) && (this.files[i].status != plupload.UPLOADING) && (this.files[i].status != plupload.DONE))
						fileNames.push(this.files[i].name);
				if (fileNames.length)
					url = webUrl + '/_layouts/roxority_UploadZen/mash.tl.aspx?op=cfe&lid=' + libID + '&r=' + Math.random() + '&dp=' + encodeURI(folderPath) + '&fn=' + encodeURIComponent(fileNames.join('|'));
				if (url) {
					while (url.length > 2083)
						if (((pos = url.lastIndexOf('|')) > 0) || ((pos = url.lastIndexOf('%7C')) > 0) || ((pos = url.lastIndexOf('&')) > 0))
							url = url.substr(0, pos);
						else
							break;
					jQuery.ajax({ 'url': url, success: function(data, status) {
						var roxUp = roxUps[ctlID];
						this.checkingExist = false;
						if (roxUp && data && data.length) {
							for (var i = 0; i < data.length; i++)
								if (jQuery.inArray(data[i], roxUp.existFiles) < 0)
									roxUp.existFiles.push(data[i]);
							roxUp.refresh(false);
						}
					}, error: function(xhr, status, err) {
						this.checkingExist = false;
					}, processData: false, global: false, dataType: 'json' });
				} else
					this.checkingExist = false;
			}
			if ((roxUpFlags != 0) && (this.files.length > roxUpFlags))
				for (var i = roxUpFlags; i < this.files.length; i++)
					this.remove(this.files[i].id, false);
		},

		cloneProp: function (fileID, prop) {
			var chk = '';
			for (var i = 0; i < this.files.length; i++)
				if (this.files[i].id == fileID) {
					chk = this.files[i][prop];
					break;
				}
			for (var i = 0; i < this.files.length; i++)
				if (this.files[i].id != fileID)
					this.files[i][prop] = chk;
			if (prop == 'unzip')
				this.existFiles = [];
			this.refresh();
		},

		close: function() {
			this.$ctl.slideUp();
			return this;
		},

		doUpload: function() {
			var iid = 0, i, theForm, theSubmit, noRebuild = true, url = location.href, pos = url.indexOf('#');
			try {
				for (var i = 0; i < this.files.length; i++)
					if (this.files[i].status == plupload.DONE) {
						this.remove(this.files[i].id, true);
						i--;
						noRebuild = false;
					} else
						this.refreshStatus(i, plupload.UPLOADING);
				for (var i = 0; i < this.plup.files.length; i++)
					this.plup.files[i].status = plupload.QUEUED;
				this.refresh(noRebuild);
				if (this.files.length) {
					this.uploading = 1;
					this.plup.start();
				}
				return this.refresh(true);
			} catch(err) {
				if (err && ((err['message'] && (err.message.indexOf('NPObject') >= 0)) || ((!err['message']) && err['number'] && (err.number == -2147467259)))) {
					alert(roxUpLoc['FlashWarn']);
					if (pos >= 0)
						url = url.substr(0, pos);
					if ((pos = url.indexOf('roxuplshow')) >= 0)
						url = url.substr(0, pos);
					if (url.indexOf('?') > 0)
						location.replace(url + '&roxuplshow=' + ctlID);
					else
						location.replace(url + '?roxuplshow=' + ctlID);
				} else
					alert(JSON.stringify(err));
			}
		},

		getStatus: function(fileName, s, file) {
			var html = '';
			if (!fileName)
				html = '<img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/bulletgray.png"/> <i>' + roxUpLoc['StatusNone'] + '</i>';
			else if (s < 0)
				html = '<img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/bulletred.png"/> <i>' + roxUpLoc['StatusCancel'] + '</i>';
			else if ((s == 0) || (s == plupload.QUEUED))
				html = '<img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/bulletyellow.png"/> <i>' + roxUpLoc['StatusQueue'] + '</i>';
			else if (s == plupload.UPLOADING)
				html = (file ? ('<img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/l.gif"/> ' + file.percent + '%...') : ('<div style="text-align: center;"><img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/k.gif"/></div>'));
			else if (s == plupload.DONE)
				html = '<img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/tick.png"/> <i>' + roxUpLoc['StatusDone'] + '</i>';
			else
				html = '<img align="bottom" src="' + webUrl + '/_layouts/images/roxority_UploadZen/exclamation.png"/> <a href="#noop' + new Date().valueOf() + Math.random() + '" onclick="alert(\''+ s + '\');">' + roxUpLoc['StatusFail'] + '</a>';
			return html;
		},

		onPropChange: function(fileID, prop, opt) {
			for (var i = 0; i < this.files.length; i++)
				if (this.files[i].id == fileID) {
					this.files[i][prop] = opt;
					break;
				}
			if (prop == 'unzip') {
				this.existFiles = [];
				this.refresh(false);
			}
		},

		refresh: function(noRebuild) {
			var fs = {}, f = -1, hasZip = 0, isZip, fnExt, isBlocked, isExist, html, fn, fl = 0, fll = 0, fi, tmp, hasHi1 = false, hasHi2 = false;
			for (var i = 0; i < this.files.length; i++)
				if (fn = this.files[i].name) {
					this.files[i].name = fn.substr(Math.max(fn.lastIndexOf('\\'), fn.lastIndexOf('/')) + 1);
					fs[i + '_' + fn] = this.files[i];
					if (this.files[i].status != plupload.DONE)
						fl++;
					fll++;
					if ('zip' == fn.substr(fn.lastIndexOf('.') + 1).toLowerCase())
						hasZip++;
				} else
					this.remove(this.files[i].id, true);
			if (!noRebuild) {
				html = '<tr><th>&nbsp;</th><th><img align="bottom" height="16" width="16" src="' + webUrl + '/_layouts/images/blank.gif"/> ' + roxUpLoc['File'] + '</th><th><img align="bottom" height="16" width="16" src="' + webUrl + '/_layouts/images/blank.gif"/> ' + roxUpLoc['Status'] + '</th>';
				if (checkInOut)
					html += ('<th' + (canChangeCheckIn ? '' : ' class="roxupcheckintd"') + '><img align="bottom" src="' + webUrl + '/_layouts/images/checkin.gif"/> ' + roxUpLoc['CheckIn'] + '</th>');
				html += ('</tr>');
				for (var k in fs) {
					fi = parseInt(k.substr(0, k.indexOf('_')));
					fn = k.substr(k.indexOf('_') + 1);
					isZip = ('zip' == (fnExt = fn.substr(fn.lastIndexOf('.') + 1).toLowerCase()));
					if (isBlocked = (jQuery.inArray(fnExt, roxBlocked) >= 0))
						hasHi2 = true;
					if (isExist = ((jQuery.inArray(fn, this.existFiles) >= 0) && ((!fs[k].unzip) || (fs[k].unzip == '0')) && (fs[k].status != plupload.UPLOADING) && (fs[k].status != plupload.DONE)))
						hasHi1 = true;
					html += ('<tr class="' + ((((++f) % 2) == 0) ? 'roxup-altrow ' : '') + (isBlocked ? 'roxuphi2' : (isExist ? 'roxuphi1' : '')) + '"><td><a class="roxup16 roxup-delicon roxup-notonupload" href="#noop' + fs[k].id + '" onclick="roxUps[\'' + ctlID + '\'].remove(\'' + fs[k].id + '\');"><span>' + (f + 1) + '</span></a></td><td nowrap="nowrap" id="' + fs[k].id + '_td"><span class="roxup16" style="background-image: url(\'' + webUrl + ((jQuery.browser.safari && !isChrome) ? '' : ((tmp = fn.substr(fn.lastIndexOf('.') + 1).toLowerCase()) ? (roxExtIcons[tmp] ? roxExtIcons[tmp] : roxExtIcons['gen']) : '/_layouts/images/warningEvent.gif')) + '\');">&nbsp;</span>&nbsp;' + (fn ? (roxFileName(fn, isZip && allowUnzip, hasZip, fs[k], ctlID, webUrl) + roxFileSize(fs[k].size)) : ('<i>' + roxUpLoc['StatusNone'] + '</i>')) + '</td><td><span id="' + fs[k].id + '_status">' + this.getStatus(fs[k].name, fs[k].status) + '</span></td>');
					if (checkInOut)
						html += ('<td' + (canChangeCheckIn ? '' : ' class="roxupcheckintd"') + '><select' + (canChangeCheckIn ? '' : ' disabled="disabled"') + ' class="' + (canChangeCheckIn ? 'roxup-notonupload' : '') + '" onchange="roxUps[\'' + ctlID + '\'].onPropChange(\'' + fs[k].id + '\', \'checkin\', this.options[this.selectedIndex].value);"><option value="">' + roxUpLoc['CheckIn0'] + '</option><option value="MinorCheckIn"' + ((fs[k].checkin == 'MinorCheckIn') ? ' selected="selected"' : '') + '>' + roxUpLoc['CheckIn1'] + '</option><option value="MajorCheckIn"' + ((fs[k].checkin == 'MajorCheckIn') ? ' selected="selected"' : '') + '>' + roxUpLoc['CheckIn2'] + '</option><option value="OverwriteCheckIn"' + ((fs[k].checkin == 'OverwriteCheckIn') ? ' selected="selected"' : '') + '>' + roxUpLoc['CheckIn3'] + '</option></select> ' + (canChangeCheckIn ? ('<a class="roxup-notonupload roxup-onhover" href="#noop' + fs[k].id + '" onclick="roxUps[\'' + ctlID + '\'].cloneProp(\'' + fs[k].id + '\', \'checkin\');"><img border="0" src="' + webUrl + '/_layouts/images/checkall.gif" alt="' + roxUpLoc['SetAll'] + '" title="' + roxUpLoc['SetAll'] + '"/></a>') : '') + '</td>');
					html += ('</tr>');
				}
			}
			jQuery('#' + ctlID + '_count').text(fl);
			jQuery('#' + ctlID + '_upload').css({ "visibility": (((this.uploading == 0) && (fl > 0)) ? "visible" : "hidden") });
			jQuery('.roxup-notonupload').attr("disabled", this.uploading > 0);
			if (this.uploading > 0) {
				jQuery('#' + ctlID + '_close').hide();
				jQuery('#' + ctlID + '_cancel').show();
			} else {
				jQuery('#' + ctlID + '_cancel').hide();
				jQuery('#' + ctlID + '_close').show();
			}
			jQuery('#' + ctlID + '_files')[hasHi1 ? 'addClass' : 'removeClass']('roxuphashi1');
			jQuery('#' + ctlID + '_files')[hasHi2 ? 'addClass' : 'removeClass']('roxuphashi2');
			if (html)
				jQuery('#' + ctlID + '_files_table').html(html);
			if (fll > 0) {
				jQuery('#' + ctlID + '_files').show();
				jQuery('#' + ctlID + '_nofiles').hide();
			} else {
				jQuery('#' + ctlID + '_files').hide();
				jQuery('#' + ctlID + '_nofiles').show();
			}
			for (var i = 0; i < this.files.length; i++)
				jQuery('#' + this.files[i].id).appendTo('#' + this.files[i].id + '_rowinput');
			this.plup.refresh();
			return this;
		},

		refreshStatus: function(i, newStatus, file) {
			if (newStatus !== null)
				this.files[i].status = newStatus;
			jQuery('#' + this.files[i].id + '_status').html(this.getStatus(this.files[i].name, this.files[i].status, file));
			return this;
		},

		remove: function(fileID, norefresh) {
			var sh = false, html, pos1, pos2;
			for (var i = 0; i < this.plup.files.length; i++)
				if (this.plup.files[i].id == fileID) {
					this.plup.removeFile(this.plup.files[i]);
					i--;
				}
			for(var i = 0; i < this.files.length; i++) {
				if (this.files[i].id == fileID)
					sh = true;
				if (sh && (i < (this.files.length - 1)))
					this.files[i] = this.files[i + 1];
			}
			if (sh)
				this.files.length = this.files.length - 1;
			return norefresh ? this : this.refresh();
		},

		show: function() {
			this.$ctl.slideDown();
			return this;
		}

	};
	roxUps[ctlID] = up;
	return up;
}

if (!roxExInt)
	roxExInt = setInterval(roxCheckExists, 2500);
