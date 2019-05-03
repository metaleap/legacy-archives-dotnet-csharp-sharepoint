var roxMulSels = {}, roxAcHint = '', roxAjax14Interval = 0, roxMulInt, roxAjax14Hide = false, roxAjax14Focus = true, roxAutoReposts = [], roxLvDone = [], roxIsDtUndefined = false, roxDtInt = 0, roxMultiLastVals = {}, roxMultis = {}, roxLox = {}, roxMultiOpsAll = '', roxMultiOpsAny = '', roxMultiOpsNone = '', roxMultiOpsNum = '', roxMultiOpsStr = '', roxMultiOpsUser = '', roxMultiDateCounts = {}, roxMultiUserCounts = {}, roxSeps = [';#'], roxNuAction = null, roxListViews = [], roxMultiSelectBusy = false, roxMainForm = document.getElementById('aspnetForm'), roxMultiMins = {}, roxListViewUrl = '', roxFilterDescs = {}, filterListBoxID, nameTextBoxID, wizardFieldDropDownListID, roxNewQueryString = null, roxUpdatePreview = function() { }, roxButtonIds = ['#ctl00_MSOTlPn_EditorZone_MSOTlPn_OKBtn', '#ctl00_MSOTlPn_EditorZone_MSOTlPn_AppBtn', '#ctl00_MSO_ContentDiv_MSOTlPn_EditorZone_MSOTlPn_OKBtn', '#ctl00_MSO_ContentDiv_MSOTlPn_EditorZone_MSOTlPn_AppBtn'], roxFilterNames = {}, roxFilterCamlOps = {}, roxFilterEmpties = [], roxLastDateCtls = {}, roxLastUserCtls = {}, roxHideDatasheetRibbon = false, roxAnim = '/_layouts/images/roxority_FilterZen/k.gif';

try {
	if (g_strDateTimeControlIDs === undefined)
		roxIsDtUndefined = true;
} catch (e) {
	roxIsDtUndefined = true;
}
if (roxIsDtUndefined)
	g_strDateTimeControlIDs = new Array();

jQuery(document).ready(function() {
	var tab, att, node, pos, elem, elems, vals = [], subVals, sv, $td, tmp, thNum, thNames = [], thChildren, thHtml, thFids = {};
	setInterval(roxNoWarn, 250);
	if (!jQuery.browser.msie)
	    jQuery('.rox-ifilter-label-datetime').css({ 'padding-bottom': '8px' });
	if (roxNewQueryString && roxMainForm) {
		if (!roxMainForm.action)
			roxMainForm.action = roxNewQueryString;
		else if ((pos = roxMainForm.action.indexOf('?')) > 0)
			roxMainForm.action = roxMainForm.action.substr(0, pos) + roxNewQueryString;
		else
			roxMainForm.action = roxMainForm.action + roxNewQueryString;
		jQuery('#aspnetForm').attr('action', roxNuAction = roxMainForm.action);
		roxRefreshFilters();
	}
	for (var i = 0; i < roxListViews.length; i++) {
		if ((jQuery.inArray(roxListViews[i].listID + '-' + roxListViews[i].viewID, roxLvDone) < 0) && ((tab = document.getElementById(roxListViews[i].listID + '-' + roxListViews[i].viewID)) || (tab = roxFindDocLibTable(roxListViews[i].viewID)))) {
			roxLvDone.push(roxListViews[i].listID + '-' + roxListViews[i].viewID);
			if (roxListViews[i].highlight && roxListViews[i].filters && roxListViews[i].filters.length)
				for (var fid in roxFilterNames) {
					for (var s = 0; s < roxSeps.length; s++)
						if ((fid.substr(0, roxListViews[i].wpID.length) == roxListViews[i].wpID) && (jQuery.inArray(roxFilterNames[fid], roxListViews[i].filters) >= 0) && ((elem = document.getElementById('filterval' + fid.substr(roxListViews[i].wpID.length))) != null) && elem.value && (elem.value != '0478f8f9-fbdc-42f5-99ea-f6e8ec702606') && ((elem.value != '0') || (elem.tagName.toLowerCase() != 'select')) && (subVals = elem.value.split(roxSeps[s])) && subVals.length) {
							if (elem.className == 'rox-multiselect ms-input')
								jQuery(elem).find('option').each(function(optIndex, option) {
									if (option.selected && (option.value) && (option.value != '0') && (option.value != '0478f8f9-fbdc-42f5-99ea-f6e8ec702606') && (subVals = option.value.split(roxSeps[s])) && subVals.length)
										if ((s == 0) && (jQuery.inArray(sv = (subVals[(subVals.length > 1) ? 1 : 0] + '').toLowerCase(), vals) < 0))
											vals.push(sv);
										else if (s > 0)
											for (var si = 0; si < subVals.length; si++)
												if (jQuery.inArray(sv = (subVals[si] + '').toLowerCase(), vals) < 0)
													vals.push(sv);
								});
							else if ((s == 0) && (jQuery.inArray(sv = (subVals[(subVals.length > 1) ? 1 : 0] + '').toLowerCase(), vals) < 0))
								vals.push(sv);
							else if (s > 0)
								for (var si = 0; si < subVals.length; si++)
									if (jQuery.inArray(sv = (subVals[si] + '').toLowerCase(), vals) < 0)
										vals.push(sv);
						}
				}
			if (vals.length) {
				for (var v = vals.length - 1; v >= 0; v--)
					if (((tmp = jQuery.inArray(vals[v], vals)) >= 0) && (tmp < v))
						vals.splice(tmp, 1);
				jQuery(tab).find('td').each(function(ix, td) {
					$td = jQuery(td);
					for (var v = 0; v < vals.length; v++)
						if ((vals[v] != 'on') && (($td.attr('class') + '').substr(0, 5) == 'ms-vb') && ($td.text().toLowerCase().indexOf(vals[v]) >= 0))
							roxHighlightNode(td, vals[v]);
				});
			}
			if (roxListViews[i].disableFilters && roxListViews[i].filters && roxListViews[i].filters.length && tab.firstChild && tab.firstChild.firstChild && tab.firstChild.firstChild.childNodes && tab.firstChild.firstChild.childNodes.length)
				for (var c = 0; c < tab.firstChild.firstChild.childNodes.length; c++)
					if (tab.firstChild.firstChild.childNodes[c].firstChild && (node = tab.firstChild.firstChild.childNodes[c].firstChild.firstChild) && node.attributes && (att = node.attributes.getNamedItem('Name')) && (jQuery.inArray(att.value, roxListViews[i].filters) >= 0))
						jQuery(node).attr('FilterDisable', 'TRUE').attr('Filterable', 'FALSE');
					else if ((node = tab.firstChild.firstChild.childNodes[c].firstChild) && node.attributes && (att = node.attributes.getNamedItem('Name')) && (jQuery.inArray(att.value, roxListViews[i].filters) >= 0))
						jQuery(node).attr('FilterDisable', 'TRUE').attr('Filterable', 'FALSE');
			if (roxListViews[i].embedFilters) {
				thChildren = jQuery(tab.firstChild.firstChild).children();
				thNum = thChildren.length;
				thHtml = '<tr>';
				for (var th = 0; th < thNum; th++) {
					if ((!(thNames[th] = jQuery(thChildren[th].firstChild.firstChild).attr("Name"))) && (!(thNames[th] = jQuery(thChildren[th].firstChild).attr("Name"))))
						thNames[th] = '';
					thHtml += '<td class="ms-vh2" id="';
					if (thNames[th])
						for (var fid in roxFilterNames)
							if ((fid.substr(0, roxListViews[i].wpID.length) == roxListViews[i].wpID) && (roxFilterNames[fid] == thNames[th])) {
								thHtml += 'rfzembed' + fid.substr(roxListViews[i].wpID.length);
								thFids[fid.substr(roxListViews[i].wpID.length)] = roxListViews[i].wpID;
								break;
							}
					thHtml += '"></td>';
				}
				thHtml += '</tr>';
				jQuery(tab.firstChild.firstChild).after(thHtml);
				for (var thFid in thFids) {
					jQuery('#rox_ifilter_control' + thFid).prependTo('#rfzembed' + thFid);
					jQuery('.rox-ifilter-all-' + thFids[thFid]).hide();
				}
			}
		}
	}
	jQuery('span.rox-ifilter-datetime td.ms-dtinput').keyup(roxOnKey);
	jQuery('span.rox-ifilter input, span.rox-ifilter select').change(function() {
		jQuery(this).parents('span.rox-ifilter').addClass ('rox-ifilter-stale');
		jQuery(this).parents('span.rox-ifilter-all').addClass ('rox-ifilter-all-stale');
	});
	if (roxAjax14Interval)
		setInterval(roxAjax14EmulateListAutoRefresh, roxAjax14Interval * 1000);
});

jQuery(window).load(function() {
	var tab, node, att, mulSels;
	for (var i = 0; i < roxListViews.length; i++)
		if (roxListViews[i].filters && roxListViews[i].filters.length && (tab = document.getElementById(roxListViews[i].listID + '-' + roxListViews[i].viewID)) && tab.firstChild && tab.firstChild.firstChild && tab.firstChild.firstChild.childNodes && tab.firstChild.firstChild.childNodes.length)
			for (var c = 0; c < tab.firstChild.firstChild.childNodes.length; c++)
				if (tab.firstChild.firstChild.childNodes[c].firstChild && (node = tab.firstChild.firstChild.childNodes[c].firstChild.firstChild) && node.attributes && (att = node.attributes.getNamedItem('Name')) && (jQuery.inArray(att.value, roxListViews[i].filters) >= 0))
					jQuery(node).attr('FilterDisable', 'TRUE');
	roxNoWarn();
	if ((mulSels = jQuery('span.rox-ifilter input.multiSelect')).length) {
		mulSels.each(function() {
			roxMulSels[this.parentNode.id] = jQuery(this).val();
		});
		roxMulInt = setInterval(roxMulSelCheck, 500);
	}
	jQuery('span.rox-ifilter-label2').each(function(i, span) {
		var fid = span.id.substr("rox_ifilter_label2_".length), ctl1 = document.getElementById('roxifiltercontrol1_' + fid);
		if (ctl1)
			jQuery('#rox_ifilter_label_' + fid).width(jQuery(ctl1).width());
	});
});

function roxAjax14EmulateListAutoRefresh() {
	if (roxAjax14Focus && !$(".roxfilterouter *:focus").length)
		return;
	jQuery('img#ManualRefresh').click();
}

function roxMulSelCheck() {
	var el, txt, refr = '';
	for (var p in roxMulSels)
		if (((el = jQuery('#' + p)).length) && ((txt = el.find('input.multiSelect')).length) && (txt.val() != roxMulSels[p])) {
			txt.parents('span.rox-ifilter').addClass ('rox-ifilter-stale');
			txt.parents('span.rox-ifilter-all').addClass ('rox-ifilter-all-stale');
			for (var i = 0; i < roxAutoReposts.length; i++)
				if (roxAutoReposts[i] == 'rox_ifilter_' + p.substr(p.lastIndexOf('_') + 1)) {
					refr = el.parent().removeClass('rox-ifilter-control').attr('class').substr('rox-ifilter-control-'.length);
					break;
				}
		}
	if (refr) {
		if (roxMulInt)
			clearInterval(roxMulInt);
		roxRefreshFilters(refr);
	}
}

function roxFindDocLibTable(viewID) {
	var tab = null;
	jQuery('table.ms-listviewtable, table.ms-summarystandardbody').each(function(i, table) {
		var $t = jQuery(table), attVal;
		try {
			attVal = $t.attr('o:WebQuerySourceHref');
		} catch(err) {
			try {
				attVal = $t.attr('WebQuerySourceHref');
			} catch(err2) {
			}
		}
		if ((attVal && (attVal.indexOf('View=' + viewID) > 0)) || (viewID.toLowerCase() == ('{' + $t.parents('div').attr('WebPartID').toLowerCase() + '}')))
			tab = table;
	});
	return tab;
}

function roxHighlightNode(node, val) {
	var html, $node = jQuery(node), lastIndex = 0, ist;
	if ((node.tagName == 'SPAN') && ($node.attr('class') == 'rox-hilitematch'))
		return '';
	else if (node.nodeName == '#text') {
		html = node.nodeValue;
		while ((lastIndex = html.toLowerCase().indexOf(val.toLowerCase(), lastIndex)) >= 0) {
			html = ((lastIndex == 0) ? '' : html.substr(0, lastIndex)) + '<span class="rox-hilitematch">' + html.substr(lastIndex, val.length) + '</span>' + html.substr(lastIndex + val.length);
			lastIndex = lastIndex + val.length + 37;
		}
		return html;
	} else {
		for (var i = 0; i < node.childNodes.length; i++)
			if ((html = roxHighlightNode(node.childNodes[i], val)) && html.length)
				jQuery(node.childNodes[i]).replaceWith(html);
		return '';
	}
}

function roxNoWarn() {
	var ribbon, itemIDs = [];
	try {
		g_bWarnBeforeLeave = false;
	} catch(exc) {
	}
	try {
		g_warnonce = 0;
	} catch(exc) {
	}
	try {
		if (SP && SP['Ribbon'] && SP.Ribbon['PageState'] && SP.Ribbon.PageState['PageStateHandler'])
			SP.Ribbon.PageState.PageStateHandler.ignoreNextUnload = true;
	} catch(exc) {
	}
	if (roxHideDatasheetRibbon) {
		if (((ribbon = document.getElementById('Ribbon.List.ViewFormat.Datasheet-Large')) || ((ribbon = jQuery('#Ribbon.List.ViewFormat.Datasheet-Large')) && ribbon.length && (ribbon = ribbon[0]))))
			jQuery(ribbon).css({ 'display': 'none' });
		jQuery('td.ms-toolbar span menu *').each(function(i, menuItem) {
			if (menuItem.id.indexOf('_EditInGridButton') > 0)
				itemIDs.push(menuItem.id);
		});
		if (itemIDs.length)
			for (var i = 0; i< itemIDs.length; i++)
				jQuery('#' + itemIDs[i]).remove();
	}
	if (roxAjax14Hide)
		jQuery('img#ManualRefresh').hide();
}

function roxClearFilters(wpid, fid, clearAct) {
    jQuery((fid ? ('#rox_ifilter_control_' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' input.ms-input').each(function(i, e) { try { jQuery(e).val(''); } catch (err) { } });
    jQuery((fid ? ('#rox_ifilter_control_' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' input.multiSelect').each(function(i, e) { try { jQuery(e).val(''); } catch (err) { } });
    jQuery((fid ? ('#rox_ifilter_control_' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' div.multiSelectOptions label input').each(function(i, e) { try { e.checked = (i == 0); } catch (err) { } });
    jQuery((fid ? ('#rox_ifilter_control_' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' select.ms-input').each(function(i, e) { try { e.selectedIndex = 0; } catch (err) { try { e.selectedIndex = -1; } catch (err2) { } } });
	jQuery((fid ? ('#rox_ifilter_control_' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' input.rox-check-value').attr('checked', false).removeAttr('checked');
	jQuery((fid ? ('#rox_ifilter_control_' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' input.rox-check-default').attr('checked', true);
	jQuery((fid ? ('div.rox-multibox-' + fid) : ('span.rox-ifilter-control-' + wpid)) + ' div.rox-multifilter').each(function(i, div) {
		var cls = div.className.split(/\s+/)[1], index = cls.substr(cls.lastIndexOf('_') + 1), len = 'rox-multifilter-'.length, id = cls.substr(len, cls.length - len - (1 + index.length));
		roxMultiChanged(id, parseInt(index), 'val', '');
	});
	if (!fid)
	    roxRefreshFilters(wpid, clearAct);
}

function roxCollapseGroups(wpID) {
	jQuery('table.ms-listviewtable td.ms-gb a').each(function(i, a) {
		var $a = jQuery(a);
		if ((a.href == 'javascript:') && (a.onclick.toString().indexOf("ExpCollGroup") > -1) && ($a.html().toLowerCase().indexOf('<img ') > -1))
			$a.click();
	});
}

function roxMultiSelect(elem, temp) {
	var minVal, elems, i;
	if (roxMultiSelectBusy)
		return;
	roxMultiSelectBusy = true;
	if ((!elem.id) && ((minVal = roxMultiMins[elem.attr('name')]) != undefined) && (elems = document.getElementsByName(elem.attr('name'))) && elems.length) {
		if ((minVal + '') == (elem.attr('value') + '')) {
			for(i = 0; i < elems.length; i++)
				if (((minVal + '') != (jQuery(elems[i]).attr('value') + '')) && jQuery(elems[i]).attr('checked')) {
					jQuery(elems[i]).attr('checked', false);
					jQuery(elems[i]).click();
					jQuery(elems[i]).attr('checked', false);
				}
			if (!elem.attr('checked')) {
				elem.attr('checked', true);
				elem.click();
				elem.attr('checked', true);
			}
		} else {
			for (i = 0; i < elems.length; i++)
				if ((minVal + '') == (jQuery(elems[i]).attr('value') + '')) {
					jQuery(elems[i]).attr('checked', false);
					jQuery(elems[i]).click();
					jQuery(elems[i]).attr('checked', false);
				}
		}
	}
	roxMultiSelectBusy = false;
}

function roxOnKey(event, wpid) {
	if (event && (event.keyCode == 13))
		roxRefreshFilters(wpid);
}

function roxRefreshFilters(wpid, clearAct) {
	roxNoWarn();
	if (wpid) {
		jQuery('#roxact_' + wpid).val('wpid_' + wpid);
		jQuery('#roxact2_' + wpid).val(clearAct ? '' : ('wpid_' + wpid));
	}
	setTimeout("showBusy();", 10);
	setTimeout("if(roxNuAction){jQuery('#aspnetForm').attr('action', roxNuAction);}if(roxMainForm)roxMainForm.submit();", 10);
}

function configInitPage(okButtonID) {
	jQuery(document).ready(function() {
		var $hidden = jQuery('#' + roxHiddenFieldID), $hidden2 = jQuery('#' + roxHidden2FieldID);
		if ($hidden2.val() != '1') {
			MSOTlPn_onToolPaneMaxClick();
			jQuery(jQuery('div.UserSectionTitle a')[0]).click();
			setTimeout("showBusy();", 10);
			$hidden2.val('1');
			if (theForm) {
				if (theForm.__EVENTTARGET) theForm.__EVENTTARGET.value = okButtonID;
				if (theForm.__EVENTARGUMENT) theForm.__EVENTARGUMENT.value = '';
				setTimeout("theForm.submit();", 500);
			}
		} else if ($hidden.val() == '1')
			toggleListWizard(roxHiddenFieldID);
	});
}

function editorUse() {
	toggleListWizard();
	jQuery('#' + nameTextBoxID).val(document.getElementById(wizardFieldDropDownListID).options[document.getElementById(wizardFieldDropDownListID).selectedIndex].value).focus();
}

function repopulateList(listID, textareaID, selVal, emptyID) {
	var tmp, textarea = document.getElementById(textareaID), tmpIndex = 1, isSel, selIndices = [], lines = jQuery(textarea).val().split("\n"), list = document.getElementById(listID), isMult = jQuery(list).attr('multiple'), selVals = (isMult ? selVal.split(',') : [selVal]), inner = '';
	if (lines && lines.length)
		for (var l = 0; l < lines.length; l++)
			if (lines[l] && lines[l].length)
				if ((tmp = jQuery.trim(lines[l])).length) {
					inner += ('<option value=\"' + tmp + '\"' + ((isSel = (jQuery.inArray(tmp, selVals) >= 0)) ? ' selected=\"selected\"' : '') + '>' + tmp + '</option>');
					if (isSel)
						selIndices.push(tmpIndex);
					tmpIndex++;
				}
	inner = '<option value=\"' + emptyID + '\"' + ((isSel = ((selVals.length == 0) || (jQuery.inArray(emptyID, selVals) >= 0))) ? ' selected=\"selected\"' : '') + '>' + roxEmpty + '</option>' + inner;
	if (isSel)
		selIndices.push(0);
	jQuery(list).html(inner);
	setTimeout(function() {
		jQuery(list).val(selVals);
	}, 25);
}

function showBusy() {
	if (roxAnim != 'NO_BLANK') {
		setTimeout("jQuery('.roxfilterouter').css({ 'background-image': \"url('" + roxAnim + "')\" });", 5);
		jQuery('.roxfilterouter').css({ 'background-image': "url('" + roxAnim + "')" });
		jQuery('#MSOTlPn_MainTD').css({ 'background': "#ffffff url('" + roxAnim + "') center center no-repeat" });
		setTimeout("jQuery('#MSOTlPn_Tbl').hide();", 5);
		jQuery('.roxfilterinner').css({ 'visibility': 'hidden' });
	}
}

function showFilterEditor(title) {
	var jq;
	jQuery('#roxfiltereditor').show();
	jQuery('#roxfilterlist').hide();
	jQuery('#roxlicsection').hide();
	jQuery('#roxeditortitle').text(title);
	jQuery(document).ready(function() {
		var done = false;
		for (var i = 0; i < roxButtonIds.length; i++)
			if ((jq = jQuery(roxButtonIds[i])) && jq.length)
				jq[0].disabled = done = true;
		if (done)
			jQuery('div.ms-toolpanefooter').append('<a href=\"#noop\" style=\"font-size: 11px; font-family: Arial, Helvetica, Sans-Serif; color: #047;\" onclick=\"jQuery(\'#roxfilterspecial\').hide();jQuery(\'#roxfilteradvanced\').hide(); jQuery(this).hide();\">' + jQuery('button#roxfiltappbtn').text() + ' / ' + jQuery('button#roxfiltdiscbtn').text() + ' &hellip;</a>');
	});
}

function toggleListWizard() {
	var $wiz = jQuery('#roxListWizard'), $hidden = jQuery('#' + roxHiddenFieldID), $link = jQuery('#roxListWizardLink'), linkText = $link.text();
	jQuery('#roxInfoFilterNames').slideToggle('fast');
	$wiz.slideToggle('fast', function() {
		if ($wiz.css('display') == 'block') {
			$hidden.val('1');
			$link.css('font-weight', 'bold').text(linkText.substr(0, linkText.length - 1) + ':').addClass('rox-info');
		} else {
			$hidden.val('');
			$link.removeClass('rox-info').css('font-weight', 'normal').text(linkText.substr(0, linkText.length - 1) + '?');
		}
	});
	location.replace('#roxtooltop');
}

function toolFilterAction(action, msg) {
	if ((!msg) || confirm(msg)) {
		setTimeout("showBusy();", 10);
		jQuery('#roxfilteraction').val(action);
		setTimeout("theForm.submit();", 500);
	}
}

function roxMultiAdd(id) {
	var multi = roxMultis[id];
	multi.cfg.push({ field: multi.defField, op: multi.defOp, lop: multi.defLop, val: '' });
	roxMultiUpdate(id);
	roxMultiRender(id);
}

function roxMultiChanged(id, index, propName, propVal) {
	var multi = roxMultis[id], fieldType = (multi.fieldTypes ? multi.fieldTypes[multi.cfg[index].field] : '');
	multi.cfg[index][propName] = propVal;
	if ((fieldType == 'User') && (propName == 'op'))
		jQuery('#' + id + '_' + index + '_userpicker').css({ "visibility": (((propVal == "Me") || (propVal == "NotMe")) ? "hidden" : "visible") });
	roxMultiUpdate(id);
}

function roxMultiCheckEditorVals(id) {
	var i, m, ppVal, dtInputs = jQuery('span.rox-multi-datepicker table.rox-dtpickertable td.ms-dtinput input.ms-input'), dtID, ppID, preID, midm, index, multi, $dtInput, $ppInput, $tmp, tmp, ppInputs = jQuery('span.rox-multi-userpicker span.ms-input table.ms-input table div.ms-inputuserfield');
	for (i = 0; i < dtInputs.length; i++) {
		$dtInput = jQuery(dtInputs[i]);
		dtID = dtInputs[i].id;
		preID = dtID.substr(0, dtID.indexOf('_DatePicker'));
		multi = null;
		mid = null;
		index = -1;
		for (m in roxMultis)
			if (roxMultis[m].ctlID == (preID + '_MultiTextBox')) {
				multi = roxMultis[mid = m];
				break;
			}
		if (multi && mid && ($tmp = $dtInput.parents('span.rox-multi-datepicker:first')).length && (tmp = $tmp[0]) && ((index = parseInt(tmp.id.substr(tmp.id.indexOf('_') + 1))) >= 0))
			roxMultiChanged(mid, index, 'val', $dtInput.val());
	}
	for (i = 0; i < ppInputs.length; i++) {
		$ppInput = jQuery(ppInputs[i]);
		ppVal = $ppInput.find('span#content:first').text();
		ppID = $ppInput.parents('td:first')[0].id;
		preID = ppID.substr(0, ppID.indexOf('_PeoplePicker'));
		multi = null;
		mid = null;
		index = -1;
		for (m in roxMultis)
			if (roxMultis[m].ctlID == (preID + '_MultiTextBox')) {
				multi = roxMultis[mid = m];
				break;
			}
		if (multi && mid && ($tmp = $ppInput.parents('span.rox-multi-userpicker:first')).length && (tmp = $tmp[0]) && ((index = parseInt(tmp.id.substr(tmp.id.indexOf('_') + 1))) >= 0))
			roxMultiChanged(mid, index, 'val', jQuery.trim(ppVal));
	}
}

function roxMultiGetEditor(id, multi, index) {
	var exist = false, ret = {}, isAc, fieldType = (multi.fieldTypes ? multi.fieldTypes[multi.cfg[index].field] : ''), isNum = ((fieldType == 'Integer') || (fieldType == 'Number') || (fieldType == 'Counter'));
	if ((fieldType == 'DateTime') && ((exist = roxLastDateCtls[id + index]) || (roxMultiDateCounts[id] < 6))) {
		if (!exist)
			roxLastDateCtls[id + index] = roxMultiDateCounts[id] = roxMultiDateCounts[id] + 1;
		ret['html'] = ('<span class="rox-multi-datepicker" id="' + id + '_' + index + '_datepicker"></span> ');
		ret['func'] = function() {
			jQuery(document.getElementById(id + '_' + index + '_datepicker')).append(document.getElementById(multi.ctlID.substr(0, multi.ctlID.lastIndexOf('_')) + '_DatePicker' + (roxLastDateCtls[id + index] - 1)));
		};
	} else if ((fieldType == 'User') && ((exist = roxLastUserCtls[id + index])  || (roxMultiUserCounts[id] < 6))) {
		if (!exist)
			roxLastUserCtls[id + index] = roxMultiUserCounts[id] = roxMultiUserCounts[id] + 1;
		ret['html'] = ('<span class="rox-multi-userpicker" id="' + id + '_' + index + '_userpicker"></span> ');
		ret['func'] = function() {
			var el = document.getElementById(id + '_' + index + '_userpicker'), $el = jQuery(el), tmp;
			$el.append(document.getElementById(multi.ctlID.substr(0, multi.ctlID.lastIndexOf('_')) + '_PeoplePicker' + (roxLastUserCtls[id + index] - 1)));
			if (((tmp = multi.cfg[index]['op']) == 'Me') || (tmp == 'NotMe'))
				$el.css({ "visibility": "hidden" });
		};
	} else if (fieldType == 'Boolean') {
		ret['html'] = '<span class="rox-multi-check-input"><input id="' + id + '_' + index + '_checkbox" value="1" class="rox-multi-input" type="checkbox" checked="checked" disabled="disabled"/> <label for="' + id + '_' + index + '_checkbox">' + roxLox['MultiChecked'] + '</label></span> ';
		ret['func'] = function() {
			roxMultiChanged(id, index, 'val', '1');
		};
	} else {
		isAc = multi.autoComplete && multi.autoComplete.active && multi.cfg[index].field && ('*' != multi.cfg[index].field);
		ret['html'] = '<input id="' + id + '_' + index + '_textbox" class="ms-input rox-multi-input rox-multi-text-input" type="text" onchange="roxMultiChanged(\'' + id + '\', ' + index + ', \'val\', jQuery(this).val());"' + (isAc ? ' autocomplete="off" onblur="roxMultiChanged(\'' + id + '\', ' + index + ', \'val\', jQuery(this).val());"' : '') + '/> ';
		if (isAc)
			ret['func'] = function() {
				jQuery('#' + id + '_' + index + '_textbox').autocomplete(multi.autoComplete.url + '&f=' + multi.cfg[index].field, multi.autoComplete.opts);
			}
	}
	return ret;;
}

function roxMultiInit(id) {
	jQuery(document).ready(function() {
		var multi = roxMultis[id], tb = document.getElementById(multi.ctlID), $tb = jQuery(tb), cfg;
		try {
			cfg = jQuery.parseJSON($tb.text());
		} catch(e) {
			cfg = null;
		}
		if ((!cfg) || !cfg.length)
			cfg = [{ field: multi.defField, op: multi.defOp, lop: multi.defLop, val: '' }];
		$tb.text(JSON.stringify(multi.cfg = cfg));
		roxMultiRender(id);
		if (!roxDtInt)
			roxDtInt = setInterval("roxMultiCheckEditorVals('" + id + "');", 500);
	});
}

function roxMultiRemove(id, index) {
	var multi = roxMultis[id];
	multi.cfg.splice(index, 1);
	roxMultiUpdate(id);
	roxMultiRender(id);
}

function roxMultiRender(id) {
	var i, theElem, editor, multi = roxMultis[id], cfg = multi.cfg, $div = jQuery('div.rox-multibox-' + id), tmp, html, ddls = ['field', 'op'];
	if (multi.allowMulti)
		ddls.push('lop');
	if (roxMultiDateCounts[id])
		for (i = 0; i < roxMultiDateCounts[id]; i++)
			if ((theElem = document.getElementById(multi.ctlID.substr(0, multi.ctlID.lastIndexOf('_')) + '_DatePicker' + i)) != null)
				jQuery(document.getElementById(multi.ctlID.substr(0, multi.ctlID.lastIndexOf('_')) + '_MultiPanel')).append(theElem);
	if (roxMultiUserCounts[id])
		for (i = 0; i < roxMultiUserCounts[id]; i++)
			if ((theElem = document.getElementById(multi.ctlID.substr(0, multi.ctlID.lastIndexOf('_')) + '_PeoplePicker' + i)) != null)
				jQuery(document.getElementById(multi.ctlID.substr(0, multi.ctlID.lastIndexOf('_')) + '_MultiPanel')).append(theElem);
	$div.html('');
	for (i = 0; i < cfg.length; i++) {
		html = '<div class="rox-multifilter rox-multifilter-' + id + '_' + i + '" style="padding-left: ' + (multi.indent ? (((i == (cfg.length - 1)) ? (i - 1) : i) * 20) : 0) + 'px;"><select class="rox-fieldsel" onchange="roxMultiChanged(\'' + id + '\', ' + i + ', \'field\', this.options[this.selectedIndex].value);roxMultiRender(\'' + id + '\');roxMultiChanged(\'' + id + '\', ' + i + ', \'op\', jQuery(\'.rox-opsel-' + id + '-' + i + '\')[0].options[jQuery(\'.rox-opsel-' + id + '-' + i + '\')[0].selectedIndex].value);">' + multi.fieldOpts + '</select> <select class="rox-opsel rox-opsel-' + id + '-' + i + '" onchange="roxMultiChanged(\'' + id + '\', ' + i + ', \'op\', this.options[this.selectedIndex].value);"' + (multi.isCaml ? '' : ' disabled="disabled"') + '>' + (multi.isCaml ? (((multi.fields && multi.fields[cfg[i].field]) ? multi.fields[cfg[i].field] : (multi.allowAnyAllOps ? roxMultiOpsAll : roxMultiOpsAny))) : roxMultiOpsNone) + '</select> ' + (editor = roxMultiGetEditor(id, multi, i, multi.autoComplete))['html'];
		if (multi.allowMulti) {
			html += ('<select class="rox-lopsel" onchange="roxMultiChanged(\'' + id + '\', ' + i + ', \'lop\', this.options[this.selectedIndex].value);"' + ((i == (cfg.length - 1)) ? ' style="visibility: hidden;"' : '') + (multi.isCaml ? '' : ' disabled="disabled"') + '><option value="And">' + roxLox['CamlOp_And'] + '</option><option value="Or">' + roxLox['CamlOp_Or'] + '</option></select> <a href="#" onclick="roxMultiAdd(\'' + id + '\');"><img align="bottom" src="/_layouts/images/roxority_FilterZen/add.png"/></a> ');
			if (cfg.length > 1)
				html += '<a href="#" onclick="roxMultiRemove(\'' + id + '\', ' + i + ');"><img align="bottom" src="/_layouts/images/roxority_FilterZen/remove.gif"/></a> ';
		}
		$div.append(html + '</div>');
		if (editor['func'])
			setTimeout(editor['func'], (i + 1));
		for (var d = 0; d < ddls.length; d++)
			if ((cfg[i][ddls[d]]) && ((ddls[d] == 'field') || (multi.isCaml)))
				for (var j = 0; j < (tmp = jQuery('div.rox-multibox-' + id + ' div.rox-multifilter-' + id + '_' + i + ' select.rox-' + ddls[d] + 'sel')[0]).options.length; j++)
					if (tmp.options[j].value == cfg[i][ddls[d]]) {
						tmp.selectedIndex = j;
						break;
					}
		jQuery('div.rox-multibox-' + id + ' div.rox-multifilter-' + id + '_' + i + ' input.rox-multi-text-input').val(cfg[i].val);
	}
}

function roxMultiUpdate(id) {
	var multi = roxMultis[id], tb = document.getElementById(multi.ctlID);
	jQuery(tb).text(JSON.stringify(multi.cfg));
}
