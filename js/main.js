var peeps = [];
var peepsByKey = { };
var groups = { };
var excludedGroups = { };
var peepsLoaded = false;

var height = window.innerHeight - 20;
var width = window.innerWidth - 20;
var getX = d3.scale.linear().domain([0,1]).range([120, width - 640]);
var getY = d3.scale.linear().domain([0,1]).range([120,height - 240]);
var getRadius = function(d) { return 15; };
var colorize = d3.scale.linear().domain([0,1]).range(["hsl(250, 50%, 50%)", "hsl(350, 100%, 50%)"]).interpolate(d3.interpolateHsl);
var del = d3.scale.linear().domain([0,1]).range([0,1]);

var canvas = $("#peeps");
var socket;
    
var drawPeep = function(data) {
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
};

var normalizePeep = function(peep) {
    if(!peep.TwitterHandle) {
        var origPeep = peepsByKey[peep.Key];
        if(origPeep) {
            peep.TwitterHandle = origPeep.TwitterHandle;
        }
    }
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
    peepsByKey[peep.Key] = peep;
    var group = groups[peep.GroupName];
    if(!group) {
        group = {
            x: getX(peep.GroupCenterX),
            y: getY(peep.GroupCenterY),
            name: peep.GroupName
        };
        groups[peep.GroupName] = group;
        
        var canvas = $("#groups");
        var groupDiv = $('<div class="group">' + group.name + '</div>');
        groupDiv.css("left", (group.x) + "px");
        groupDiv.css("top", (group.y) + "px");
        groupDiv.attr("id", "group-" + group.name);
        canvas.append(groupDiv);
    }
    var data = normalizePeep(peep);
    peeps.push(data);
    if(!peepsLoaded) {
        drawPeep(data);
    }
};

var modifyPeep = function(data) {
    var peep = normalizePeep(data);
    var peepDiv = $('#' + peep.handle);
    var changes = { };
    if(peep.x) {
        changes.left = getX(peep.x);
    }
    if(peep.y) {
        changes.top = getY(peep.y);
    }
    peepDiv.animate(
        changes,
        {
            duration: 500
        });
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

var connectToApi = function(callback) {
    
    socket = io.connect("https://api.cerrio.com:443");
    
    socket.on("peeps", function(update){
       updatePeep(update);
    });
    
    // First, clear out any existing subscription for this twitter usre
    socket.emit("update", {
        id: 'login',
        uri: 'SDC/Input/Users',
        action: 'delete',
        itemKey: username
    });
    
    socket.emit("update", {
        id: 'login',
        uri: 'SDC/Input/Users',
        action: 'add',
        itemKey: username,
        item: {
            Handle: username,
            HashTag: hashtag,
            DeletedTerms: '',
            TwitterSearchKey: '' // (cerrio ? cerrio.client.uid : username)
        }
    });
    
    socket.emit("stream", {
        id: "peeps",
        uri: "SDC/Output/Data",
        subscription: "True"
    },
    callback);
    
};


var showUserTweets = function(handle) {
    var twitterbox = $("#twitter-box");
    twitterbox.css('visibility', 'visible');
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

var excludedWords = [];

var loadPeepsDom = function() {
    peepsLoaded = true;
    canvas = $("#peeps");
    $(".group").click(function(){
        var groupName = $(this).text();
        $(this).hide("explode", 1500);
        excludedWords.push(groupName);
        socket.emit("update", {
            id: 'login',
            uri: 'SDC/Input/Users',
            action: 'modify',
            itemKey: username,
            item: {
                DeletedTerms: excludedWords.join(',')
            }
        });
        
    });
    var i;
    for(i = 0; i < peeps.length; i++) {
        var data = peeps[i];
        drawPeep(data);
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