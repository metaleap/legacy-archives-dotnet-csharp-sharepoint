var prodName = '', prodNameLower = '', inner = '', roxCfgPageTitle, picHeights = {}, roxAnimInterval1, roxAnimInterval2, roxAnimSpeed = 250;
jQuery(document).ready(function() {
	var $elems;
	if ((location.href.toLowerCase().indexOf('_layouts/roxority_') >= 0) || (location.href.toLowerCase().indexOf('_layouts/yukka_') >= 0)) {
		if (location.href.toLowerCase().indexOf('/default.aspx') >= 0)
			jQuery('span#roxconfiglink').html('<a href="?cfg=cfg">' + roxCfgPageTitle + '</a>');
		$elems = jQuery('img.rox-docpic');
		$elems.wrap(function() { return '<a class="top_up" href="' + this.src + '" toptions="shaded = 1, modal = 1, overlayClose = 0, resizable = 0, effect = switch"/>'; });
		jQuery('#roxDocToc').html(inner);
		$elems = jQuery('#licdiv li');
		if ($elems.length > 1) {
			$elems[0].style.backgroundImage = "url('/_layouts/images/share.gif')";
			$elems[1].style.backgroundImage = "url('/_layouts/images/cer16.gif')";
			$elems[2].style.backgroundImage = "url('/_layouts/roxority_RollupZen/img/lstpend.gif')";
		}
	}
});
function roxClearCssFilter() {
	var $this = jQuery(this), attr = $this.attr('style'), pos;
	if (attr && ((pos = attr.toLowerCase().indexOf('filter:')) >= 0))
		$this.attr('style', (pos == 0) ? ('x' + attr.substr(1)) : (attr.substr(0, pos) + 'x' + attr.substr(pos + 1)));
}
function roxGoLicense() {
	window.open(roxBaseUrl + prodNameLower + '-license/');
}
function roxGoReset(name, scope) {
	var elem = document.getElementById('cfg_' + scope + '_' + name);
	document.getElementById('chk_' + scope + '_' + name).disabled = elem.disabled = true;
	location.replace('?cfg=reset&name=' + name + '&scope=' + scope);
}
function roxGoSave(name, scope) {
	var elem = document.getElementById('cfg_' + scope + '_' + name), value = ((elem.tagName == 'SELECT') ? elem.options[elem.selectedIndex].value : ((jQuery(elem).attr('type').toLowerCase() == 'checkbox') ? (elem.checked ? 'true' : 'false') : jQuery(elem).val()));
	document.getElementById('chk_' + scope + '_' + name).disabled = elem.disabled = true;
	location.replace('?cfg=save&name=' + name + '&scope=' + scope + '&value=' + escape(value));
}
function roxGoSite(url) {
	var pos = location.href.indexOf('#'), pos2 = location.href.indexOf('?');
	location.href = (((pos < 0) ? location.href : location.href.substr(0, pos)) + ((pos2 < 0) ? '?' : '&') + "roxgosite=" + escape(url));
}
function roxGoWeb(id) {
	var pos = location.href.indexOf('#'), pos2 = location.href.indexOf('?');
	location.href = (((pos < 0) ? location.href : location.href.substr(0, pos)) + ((pos2 < 0) ? '?' : '&') + "roxgoweb=" + escape(id));
}
function roxHintSite(show, startPos) {
	jQuery('#rox_sitehint').css({ "visibility": (show ? "visible" : "hidden") });
	if (roxAnimInterval1 && roxAnimInterval2 && !show) {
		clearInterval(roxAnimInterval1);
		clearInterval(roxAnimInterval2);
	} else if (show) {
		roxAnimInterval1 = setInterval(function() {
			jQuery('#rox_sitehint').fadeOut(roxAnimSpeed).animate({ "top": (startPos ? startPos : "320") }, roxAnimSpeed);
		}, roxAnimSpeed);
		setTimeout(function () {
			roxAnimInterval2 = setInterval(function() {
				jQuery('#rox_sitehint').fadeIn(roxAnimSpeed).animate({ "top": "32" }, roxAnimSpeed);
			}, roxAnimSpeed);
		}, roxAnimSpeed);
	}
}
function roxSlimEncode(val) {
	var s = '';
	for (var i = 0; i < val.length; i++)
		switch (val[i]) {
			case '%':
				s += '%25';
				break;
			case '+':
				s += '%2B';
				break;
			case ' ':
				s += '%20';
				break;
			case '?':
				s += '%3F';
				break;
			case '&':
				s += '%26';
				break;
			case '=':
				s += '%3D';
				break;
			case '\t':
				s += '%09';
				break;
			case '\r':
				s += '%0D';
				break;
			case '\n':
				s += '%0A';
				break;
			default:
				s += val[i];
		}
	return s;
}
