export function reparseUnobtrusive(form) {
  if (!form) return;
  if (!window.jQuery || !$.validator || !$.validator.unobtrusive) return;
  const $f = $(form);
  $f.removeData('validator').removeData('unobtrusiveValidation');
  $.validator.unobtrusive.parse($f);
}