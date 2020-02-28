

$(function(){
	var winwidth = window.innerWidth;
	$('.sidenav__header .menu-touch').click(function() {
        //-- .stop(true,false) 點了x就會關掉
        //-- 第一個是點了停止前面的動畫
        //-- 第二個true是 要不要把當前動畫執行完 
        $('.sidenav__content').stop().slideToggle();
        $(this).parents('.sidenav__header').toggleClass('open');

        //--點menu list會關閉menu
        if(winwidth <= 768){
            $('.sidenav__item').click(function(){
                $(this).closest('.sidenav__content').css("display","none");
                $(this).parents().siblings('.sidenav__header').removeClass('open');
            });
        }
        
    });
    

    //-- 頁面底端滑到最上面
    $('.gotop').click(function(){
        $('.help article').animate({
            scrollTop: 0
        },400); 
    });

    $(window).resize(function(){
        var winwidth = window.innerWidth; //計算包含scrollerbar
        console.log(winwidth); //計算螢幕寬度
        if(winwidth > 768){
            $('.sidenav__content').css("display","block");
                $('.sidenav__item').click(function(){
                    $(this).closest('.sidenav__content').css("display","block");
                });   
        }
        else{
            $('.sidenav__content').css("display","none");
            $('.sidenav__item').click(function(){
                $(this).closest('.sidenav__content').css("display","none");
                $(this).parents().siblings('.sidenav__header').removeClass('open');
            });
        }
    });
});



