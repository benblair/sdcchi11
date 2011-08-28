var peeps = [];
var mainTag = "#SDCChi";

var normalizePeep = function(peep) {
    return {
        x: peep.X,
        y: peep.Y,
        pic: peep.ProfilePic,
        handle: peep.TwitterHandle,
        name: peep.RealName,
        group: peep.GroupName
    };
};

var addPeep = function(peep) {
    peeps.push(normalizePeep(peep));
};

var modifyPeep = function(peep) {
    
};

var updatePeep = function(update) {
    var action = update.action;
    var item = update.item;
    switch(action) {
        case 'add':
                addPeep(item);
            break;
        case 'modify':
                modifyPeep(item);
            break;
        case 'delete':
            break;
        default:
            return;
    }
};
/*
var simulatePeeps = function(callback) {
    var i;
    for(i = 0; i < 250; i++) {
        addPeep({
            X: Math.random(),
            Y: Math.random(),
            ProfilePic: "https://si0.twimg.com/profile_images/1453831880/profile-pic_normal.jpg",
            Words: "Foo,Bar,Social,Mobile"
        });
    }
    callback();
};
*/
var connectToApi = function(callback) {
    
    var socket = io.connect("https://api.cerrio.com:443");
    
    socket.on("peeps", function(update){
       updatePeep(update);
    });
    
    socket.emit("stream", {
        id: "peeps",
        uri: "SDC/Output/Data",
        subscription: "True"
    },
    callback);
    
};

var height = window.innerHeight - 20;
var width = window.innerWidth - 20;
var getX = d3.scale.linear().domain([0,1]).range([60, width - 380]);
var getY = d3.scale.linear().domain([0,1]).range([80,height - 80]);
var getRadius = function(d) { return 15; }; // d3.scale.linear().domain([0,1]).range([5,10]);
var colorize = d3.scale.linear().domain([0,1]).range(["hsl(250, 50%, 50%)", "hsl(350, 100%, 50%)"]).interpolate(d3.interpolateHsl);
//var y2 = d3.scale.linear().domain([0,1]).range([height * 0.2 - 20, height * 0.8 + 20]);
var del = d3.scale.linear().domain([0,1]).range([0,1]);

var showUserTweets = function(handle) {
    var twitterbox = $("#twitter-box");
    /*
    var newBox = [
        '',
        '<script>',
        'var height = 600;',
        'new TWTR.Widget({',
        '  version: 2,',
        '  type: "profile",',
        '  rpp: 10,',
        '  interval: 30000,',
        '  width: 250,',
        '  height: 600,',
        '  theme: {',
        '    shell: {',
        '      background: "#333333",',
        '      color: "#ffffff"',
        '    },',
        '    tweets: {',
        '      background: "#000000",',
        '      color: "#ffffff",',
        '      links: "#6da9d1"',
        '    }',
        '  },',
        '  features: {',
        '    scrollbar: true,',
        '    loop: false,',
        '    live: true,',
        '    hashtags: true,',
        '    timestamp: true,',
        '    avatars: false,',
        '    behavior: "all"',
        '  }',
        '}).render().setUser("' + handle + '").start();',
        '</script>'
    ].join('\n');
    twitterbox.empty();
    twitterbox.append(newBox);
    */
    if(twtr) {
        twtr
            .destroy()
            .setFeatures({
                scrollbar: true,
                loop: false,
                live: true,
                hashtags: true,
                timestamp: true,
                avatars: false,
                behavior: 'all'
            })
            .setDimensions(250, 600)
            .setRpp(10)
            .setTweetInterval(30000)
            .setUser(handle)
            .render()
            .start();
    }
};

var loadPeepsDom = function() {
    $(".group").click(function(){
        $(this).hide("explode", 1500);
    });
    var canvas = $("#peeps");
    var i;
    for(i = 0; i < peeps.length; i++) {
        var data = peeps[i];
        var newPeep = $('<img />');
        newPeep.attr("src", data.pic);
        newPeep.attr("title", data.handle);
        newPeep.attr("alt", data.handle);
        newPeep.attr("width", "50px");
        newPeep.attr("height", "50px");
        var peepDiv = $('<div class="peep"></div>');
        peepDiv.css("left", (width / 2) + "px");
        peepDiv.css("top", (height / 2) + "px");
        peepDiv.attr("id", data.handle);
        peepDiv.append(newPeep);
        canvas.append(peepDiv);
    }
    $(".peep").click(function(e) {
        
        showUserTweets(e.currentTarget.id);
    });
    setTimeout(function() {
        for(i = 0; i < peeps.length; i++) {
            var data = peeps[i];
            var peepDiv = $("#" + data.handle);
            peepDiv.animate({
                left: getX(data.x),
                top: getY(data.y)
            },
            {
                duration: 500
            });   
        }
    }, 1000);
};


var loadPeeps = function() {
    connectToApi(loadPeepsDom);
};