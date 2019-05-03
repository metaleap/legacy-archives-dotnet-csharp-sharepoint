var prodName = '', prodNameLower = '', inner = '', roxCfgPageTitle, picHeights = {}, roxAnimInterval1, roxAnimInterval2, roxAnimSpeed = 250;
jQuery(document).ready(function() {
	var $elems;
	if ((location.href.toLowerCase().indexOf('_layouts/roxority_') >= 0) || (location.href.toLowerCase().indexOf('_layouts/yukka_') >= 0)) {
		if (location.href.toLowerCase().indexOf('/default.aspx') >= 0)
			jQuery('span#roxconfiglink').html('<a href="?cfg=cfg">' + roxCfgPageTitle + '</a>');

		$elems = jQuery('img.rox-docpic');
		$elems.wrap(function() { return '<a class="image-popup-fit-width" href="' + this.src + '"/>'; });

		jQuery('.image-popup-fit-width').magnificPopup({
			type: 'image',
			closeOnContentClick: true,
			image: { verticalFit: false }
		});
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
	var elem = document.getElementById('cfg_' + scope + '_' + name), $elem = jQuery(elem), isSel = (elem.tagName == 'SELECT'), value = ((('' + $elem.attr('type')).toLowerCase() == 'checkbox') ? (elem.checked ? 'true' : 'false') : $elem.val());
	document.getElementById('chk_' + scope + '_' + name).disabled = elem.disabled = true;
	location.replace('?cfg=save&name=' + name + '&scope=' + scope + '&value=' + encodeURIComponent(value));
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
	var s = '', t;
	if (val && val["length"])
		for (var c = 0; c < val.length; c++)
			if ((t = val.substr(c, 1)) == '%')
				s += '%25';
			else if (t == '+')
				s += '%2B';
			else if (t == ' ')
				s += '%20';
			else if (t == '?')
				s += '%3F';
			else if (t == '&')
				s += '%26';
			else if (t == '=')
				s += '%3D';
			else if (t == '\t')
				s += '%09';
			else if (t == '\r')
				s += '%0D';
			else if (t == '\n')
				s += '%0A';
			else if (t == '#')
				s += '%23';
			else
				s += t;
	return s;
}
function roxSlimAttributeEncode(val) {
	var s = '', t;
	if (val && val["length"])
		for (var c = 0; c < val.length; c++)
			if ((t = val.substr(c, 1)) == '\'')
				s += '&#39;';
			else if (t == '\"')
				s += '&quot;';
			else if (t == '>')
				s += '&gt;';
			else if (t == '<')
				s += '&lt;';
			else if (t == '&')
				s += '&amp;';
			else
				s += t;
	return s;
}
function roxTogDocTab(tab) {
	jQuery('span.rox-doctab').hide();
	jQuery('span.rox-doctab-' + tab).css({ "display": "inline" });
}
