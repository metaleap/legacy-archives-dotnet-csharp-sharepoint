var roxFilterFunc = null, roxRollNoAjax = false, roxEditMode = false, roxEmbedMode = 'merge', roxFilterMoved = false, roxRollupConns = {}, roxLastOps = {}, roxRowInfoShown = false, roxNoMouseOut = false, imnFunc = null;

function roxEqualHeights() {
	jQuery(this).each(function(){
		var currentTallest = 0;
		jQuery(this).find('.rox-userprofile').each(function(i){
			if (jQuery(this).height() > currentTallest)
				currentTallest = jQuery(this).height();
		});
		jQuery(this).find('.rox-userprofile').css({ 'height': (currentTallest + 20) + 'px' }); 
	});
	return this;
}
function roxImageError(img, defImgUrl) {
	var div = jQuery(img).parent().parent();
	img.src = defImgUrl;
	div.addClass('rox-image-error');
}
function roxScrollEnd(textArea) {
	if (document.selection) {
		var sel = textArea.createTextRange();
		sel.collapse(false);
		sel.select();
    }
    textArea.focus();
}
function roxFixupTileHeights(wpID) {
	if (jQuery('#rox_rollup_' + wpID + ' div.rox-userprofiles div.rox-userprofile').length) {
		if (!jQuery.fn.equalHeights)
			jQuery.fn.equalHeights = roxEqualHeights;
		jQuery('#rox_rollup_' + wpID + ' div.rox-userprofiles').equalHeights();
	}
}
function roxPopup(url, sp14popup) {
	var fn = null;
	try {
		fn = OpenPopUpPage;
	} catch(err) {
	}
	if (sp14popup && fn)
		fn(url);
	else if ('showModalDialog' in window)
		window.showModalDialog(url, null, "center: yes; status: no; resizable: yes; help: no; dialogWidth: 890px; dialogHeight: 550px; modal: yes;");
	else if ('openDialog' in window)
		window.openDialog(url, '_blank', "height=550, width=890, center=yes, centerscreen=yes, menubar=no, toolbar=no, location=no, directories=no, personalbar=no, status=no, resizable=yes, modal=yes");
	else
		window.open(url, '_blank', "height=550, width=890, center=yes, centerscreen=yes, menubar=no, toolbar=no, location=no, directories=no, personalbar=no, status=no, resizable=yes, modal=yes");
}
function roxRefreshRollup(textAreaID, wpID, opt) {
	var text = jQuery('#' + textAreaID).text();
	roxLastOps[wpID] = [textAreaID, opt];
	if ((roxEmbedMode != 'no') && (!(roxEditMode || roxFilterMoved)))
		for (var p in roxRollupConns)
			if (p == wpID) {
				jQuery('#rox_loader_' + wpID).parent().prepend(jQuery('#roxfilterinner_' + roxRollupConns[p]).parent());
				if (roxEmbedMode == 'merge')
					jQuery('.rox-ifilter-all-' + roxRollupConns[p]).css({ 'border-top': '0px none red' });
				else {
					setTimeout("jQuery('#roxfilterinner_" + roxRollupConns[p] + "').parent().hide();", 10);
					setTimeout("jQuery('#roxfilterinner_" + roxRollupConns[p] + "').parent().show();", 100);
				}
				setTimeout("try { jQuery('select.rox-multiselect').multiSelect({ selectAll: false, noneSelected: '', oneOrMoreSelected: '*', selectAllText: '' }, roxMultiSelect); } catch(err) {}", 200);
				roxRewriteFilterFunc(true);
			}
	if (text) {
		jQuery('#rox_loader_' + wpID).hide();
		jQuery('#rox_pager_' + wpID).css({ 'background-image': 'none' });
		jQuery('#rox_rollup_' + wpID).html(text);
		setTimeout(function() { roxFixupTileHeights(wpID); }, 1);
		if (opt['fid'])
			jQuery('#roxfilterinner_' + opt.fid + ' a, #roxfilterinner_' + opt.fid + ' button, #roxfilterinner_' + opt.fid + ' input, #roxfilterinner_' + opt.fid + ' select').each(function(i, el) { el.disabled = false; });
	} else
		roxReloadRollup(textAreaID, wpID, opt);
}
function roxRefreshRollupFilters(wpID) {
	var pIDs = [], opt, f = [], fnu, fid, elem, val, elName, oneFailed = false, pos;
	for (var pID in roxRollupConns)
		if ((roxRollupConns[pID] == wpID) && (jQuery.inArray(pID, pIDs) < 0))
			pIDs.push(pID);
	if (!pIDs.length)
		roxFilterFunc(wpID);
	else {
		jQuery('#roxfilterinner_' + wpID + ' a, #roxfilterinner_' + wpID + ' button, #roxfilterinner_' + wpID + ' input, #roxfilterinner_' + wpID + ' select').each(function(i, el) { el.disabled = true; });
		for (var i = 0; i < pIDs.length; i++) {
			opt = roxLastOps[pIDs[i]][1];
			opt.fid = wpID;
			opt.p = 0;
			opt.tv = null;
			jQuery('.rox-ifilter-control-' + wpID + ' input, .rox-ifilter-control-' + wpID + ' select').each(function(i, el) {
				if ((elName = el.id) || ((elName = el.name) && (el.type == 'checkbox') && el.checked)) {
					if ((pos = elName.indexOf(wpID + '_datePicker')) > 0)
						elName = 'filterval_' + (elName = elName.substr(pos + (wpID + '_datePicker').length)).substr(0, elName.indexOf('_datePicker'));
					if ((val = jQuery(el).val()) && (val == '0478f8f9-fbdc-42f5-99ea-f6e8ec702606'))
						val = '';
					if ((pos = val.indexOf(';#')) >= 0) {
						val = val.substr(pos + 2);
					}
					if ((jQuery.inArray(fid = wpID + '_' + elName.substr('filterval_'.length), roxFilterEmpties) >= 0) || val)
						f.push([roxFilterNames[fid], val, roxFilterCamlOps[fid]]);
				}
			});
			opt.f = roxSlimEncode(JSON.stringify(f));
			if (!roxReloadRollup(roxLastOps[pIDs[i]][0], pIDs[i], opt, true))
				oneFailed = true;
		}
		if (oneFailed)
			roxFilterFunc(wpID);
	}
}
function roxReloadRollup(textAreaID, wpID, opt, ret) {
	var pager = jQuery('#rox_pager_' + wpID), url = opt.webUrl + '/_layouts/roxority_RollupZen/mash.tl.aspx?dsid=' + opt.dsid + '&tid=' + textAreaID + '&id=' + wpID + '&ps=' + opt.ps + '&p=' + opt.p + '&pmo=' + opt.pmo + '&pst=' + opt.pst + '&psk=' + opt.psk + '&dty=' + opt.dty + '&did=' + opt.did + '&la=' + roxSlimEncode(opt.la) + '&pr=' + opt.pr + '&on=' + opt.on + '&vc=' + opt.vc + '&ih=' + opt.ih + '&ls=' + opt.ls + '&v=' + opt.v + '&s=' + opt.s + '&spn=' + opt.spn + '&sd=' + opt.sd + '&tpn=' + opt.tpn + '&tpo=' + opt.tpo + ((opt.tv == null) ? '' : ('&tv=' + opt.tv)) + '&gpn=' + opt.gpn + '&gb=' + opt.gb + '&gs=' + opt.gs + '&gi=' + opt.gi + '&ti=' + opt.ti + '&gid=' + opt.gid + '&gd=' + opt.gd + '&rs=' + opt.rs + '&t=' + opt.t + '&nm=' + opt.nm + '&pm=' + opt.pm + '&f=' + opt.f + '&fa=' + opt.fa + '&fl=' + opt.fl + '&dyn=' + opt.dyn + '&r=' + Math.random();
	roxLastOps[wpID] = [textAreaID, opt];
	if (pager.length)
		pager.css({ 'background-image': "url('/_layouts/images/roxority_RollupZen/" + opt.la + ".gif')" });
	else
		jQuery('#rox_loader_' + wpID).show();
	jQuery('#rox_rollup_' + wpID + ' *').css({ color: '#808080' });	
	jQuery('#rox_rollup_' + wpID + ' tr.ms-viewheadertr a, #rox_pager_' + wpID + ' a').each(function(i, a) {
		a.disabled = true;
		a.href = '#noop' + Math.random();
		a.onclick = '';
	});
	if (roxRollNoAjax) {
		jQuery('#' + textAreaID).text('roxrollnoajax::' + url);
		theForm.submit();
	} else {
		if (ret && (url.length > 2083))
			return false;
		if (url.length > 2083)
			url = (url = url.substr(0, 2083)).substr(0, url.lastIndexOf('&'));
		jQuery.ajax({ 'url': url, success: function(data, status) {
			if (!data)
				alert('Unexpected empty server response -- please contact ROXORITY technical support!\nStatus: ' + status + '\nURL: ' + url);
			else {
				jQuery('#' + textAreaID).text(data);
				roxRefreshRollup(textAreaID, wpID, opt);
			}
		}, error: function(xhr, status, err) {
			var msg = 'Status: ' + status + '\nError: ' + err, tmp;
			if (xhr)
				for (var p in xhr)
					if (xhr[p] && (typeof(xhr[p]) != 'function') && (tmp = xhr[p] + '') && (tmp.indexOf('\n') < 0))
						msg += ('\nxhr.' + p + ': ' + tmp);
			url += '\n\nURL: ' + url;
			if (confirm(msg + '\n\nRetry?'))
				roxReloadRollup(textAreaID, wpID, opt);
		}, processData: false, global: false, dataType: 'html' });
	}
	return true;
}
function roxRewriteFilterFunc(force) {
	var rrf = null;
	try {
		rrf = roxRefreshFilters;
	} catch(e) {
	}
	if (rrf && (rrf != roxRefreshRollupFilters) && ((!roxFilterFunc) || force)) {
		roxFilterFunc = rrf;
		roxRefreshFilters = roxRefreshRollupFilters;
	}
}
