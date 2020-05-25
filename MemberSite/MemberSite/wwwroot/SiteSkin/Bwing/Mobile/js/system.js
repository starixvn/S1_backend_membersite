$(document).ready(function () {
    $('.toggle').click(function(e) {
        e.preventDefault();
    
      var $this = $(this);
      $this.toggleClass('active');
      $this.next().slideToggle(350);      
  });

  $('#btn-edit-profile').click(function() {
      $('.form-profile').addClass('hide');
      $('.form-update-profile').removeClass('hide');
  });
});